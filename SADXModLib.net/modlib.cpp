#include <windows.h>
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <string>
#include "programming/SADXModInfo.h"
#include "programming/SADXModLoader.h"
#include <iostream>

namespace
{
    hostfxr_initialize_for_runtime_config_fn init_fptr = nullptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr = nullptr;
    hostfxr_close_fn close_fptr = nullptr;
    
    void *load_library(const char_t *path)
    {
        HMODULE h = ::LoadLibraryW(path);
        return (void *)h;
    }

    void *get_export(void *h, const char *name)
    {
        void *f = (void *)::GetProcAddress((HMODULE)h, name);
        return f;
    }
}

bool load_hostfxr(const std::wstring &localDotnetDir)
{
    std::wstring localHostfxr = localDotnetDir + L"\\hostfxr.dll";
    void *lib = load_library(localHostfxr.c_str());
    
    if (!lib)
    {
        char_t buffer[MAX_PATH];
        size_t buffer_size = sizeof(buffer) / sizeof(char_t);
        if (get_hostfxr_path(buffer, &buffer_size, nullptr) == 0)
        {
            lib = load_library(buffer);
        }
        else
        {
            PrintDebug("Failed to get hostfxr path!\n");
        }
    }

    if (!lib)
        return false;

    init_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
    get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
    close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

    return (init_fptr && get_delegate_fptr && close_fptr);
}

load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *config_path, const char_t *dotnet_root, int *out_rc)
{
    void *load_assembly_and_get_function_pointer = nullptr;
    hostfxr_handle cxt = nullptr;

    int rc = init_fptr(config_path, nullptr, &cxt);
    // 0 (Success), 1 (Success_HostAlreadyInitialized), and 2 (Success_DifferentRuntimeProperties)
    // Success codes just for so I know what they are on hand
    if ((rc != 0 && rc != 1 && rc != 2) || cxt == nullptr)
    {
        if (out_rc) *out_rc = rc;
        if (cxt) close_fptr(cxt);
        return nullptr;
    }

    rc = get_delegate_fptr(cxt, hdt_load_assembly_and_get_function_pointer, &load_assembly_and_get_function_pointer);
    if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
    {
        if (out_rc) *out_rc = rc;
    }

    close_fptr(cxt);
    return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
}

typedef int (__cdecl *init_managed_fn)(const char *path);
typedef int (__cdecl *on_frame_managed_fn)();

namespace
{
    init_managed_fn managed_init_fptr = nullptr;
    on_frame_managed_fn managed_onFrame_fptr = nullptr;

    // Helper to convert SADX's ANSI/UTF-8 mod folder path into Windows UTF-16 (wchar_t), important for dotnet string since dotnet uses a 16 bit string.
    std::wstring to_wstring(const char *str)
    {
        if (!str || !*str) return L"";
        int len = MultiByteToWideChar(CP_UTF8, 0, str, -1, nullptr, 0);
        std::wstring wstr(len - 1, L'\0');
        MultiByteToWideChar(CP_UTF8, 0, str, -1, &wstr[0], len);
        return wstr;
    }
}

extern "C" {
    __declspec(dllexport) void __cdecl Init(const char *path, const struct HelperFunctions &helperFunctions)
    {
        std::wstring modDir = to_wstring(path);
        std::wstring localDotnet = modDir + L"\\dotnet";
        
        if (!load_hostfxr(localDotnet))
        {
            MessageBoxA(nullptr, "No .NET 10 x86 runtime installed", "SADXModLib.net", MB_ICONERROR);
            PrintDebug("No .NET 10 x86 runtime installed");
            return;
        }

        // Point to the self-contained runtimeconfig.json and DLL inside dotnet
        std::wstring configPath = localDotnet + L"\\SADXModLib.net.runtimeconfig.json";
        std::wstring assemblyPath = localDotnet + L"\\SADXModLib.net.dll";

        int init_rc = 0;
        load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer =
            get_dotnet_load_assembly(configPath.c_str(), localDotnet.c_str(), &init_rc);

        if (!load_assembly_and_get_function_pointer)
        {
            char err[128];
            wsprintfA(err, "Failed to initialize .NET 10 runtime! HRESULT: 0x%08X", init_rc);
            MessageBoxA(nullptr, err, "SADXModLib.net", MB_ICONERROR);
            return;
        }

        // Load SADXModLib.net.dll and resolve [UnmanagedCallersOnly] ModLib.Init
        // Format: "<Namespace>.<ClassName>, <AssemblyName>"
        const char_t *dotnet_type = L"SADXModLib.net.ModLib, SADXModLib.net";
        const char_t *dotnet_type_method_Init = L"Init";
        const char_t *dotnet_type_method_OnFrame = L"OnFrame";
        
        int rc = load_assembly_and_get_function_pointer(
            assemblyPath.c_str(),
            dotnet_type,
            dotnet_type_method_Init,
            UNMANAGEDCALLERSONLY_METHOD, // Required because Init has [UnmanagedCallersOnly]
            nullptr,
            (void **)&managed_init_fptr);

        rc = load_assembly_and_get_function_pointer(
            assemblyPath.c_str(),
            dotnet_type,
            dotnet_type_method_OnFrame,
            UNMANAGEDCALLERSONLY_METHOD, // Required because Init has [UnmanagedCallersOnly]
            nullptr,
            (void **)&managed_onFrame_fptr);

        if (rc != 0 || !managed_init_fptr)
        {
            MessageBoxA(nullptr, "Failed to get function pointer to ModLib.Init!", "SADXModLib.net", MB_ICONERROR);
            return;
        }
        
        if (rc != 0 || !managed_onFrame_fptr)
        {
            MessageBoxA(nullptr, "Failed to get function pointer to ModLib.OnFrame!", "SADXModLib.net", MB_ICONERROR);
            return;
        }

        // Call C# Init(IntPtr path) and verify the return value (34)
        int result = managed_init_fptr(path);

        char msg[128];
        wsprintfA(msg, "C# ModLib.Init returned: %d", result);
        MessageBoxA(nullptr, msg, "SADXModLib.net Hello World", MB_OK);
        PrintDebug(msg);
    }

    __declspec(dllexport) void __cdecl OnInitEnd()
    {
        // Executed after every mod has been initialized, mainly used to check if a specific mod is also enabled.
    }

    __declspec(dllexport) void __cdecl OnFrame()
    {
        // Executed every running frame of SADX
        managed_onFrame_fptr();
    }

    __declspec(dllexport) void __cdecl OnInput()
    {
        // Executed before the game processes input
    }

    __declspec(dllexport) void __cdecl OnControl()
    {
        // Executed when the game processes input
    }

    __declspec(dllexport) void __cdecl OnRenderDeviceReset()
    {
        // Executed when the window size changes
    }

    __declspec(dllexport) void __cdecl OnRenderDeviceLost()
    {
        // Executed when the game fails to render the scene
    }

    __declspec(dllexport) void __cdecl OnRenderSceneStart()
    {
        // Executed before the game starts rendering the scene
    }

    __declspec(dllexport) void __cdecl OnRenderSceneEnd()
    {
        // Executed when the game finishes rendering the scene
    }
	    
    __declspec(dllexport) void __cdecl OnExit()
    {
        // Executed when the game is about to terminate
    }

    __declspec(dllexport) struct ModInfo SADXModInfo = { ModLoaderVer }; // This is needed for the Mod Loader to recognize the DLL.
}