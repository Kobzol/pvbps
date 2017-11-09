#include <iostream>
#include <vector>

#include <Windows.h>
#include <TlHelp32.h>

static const char* PIPE_NAME = "\\\\.\\pipe\\Pipe";

DWORD WINAPI ThreadFunc(void* ptr)
{
	return 100;
}
void WINAPI ThreadFund()
{

}

void injectRemoteThreadDll(const char* dllPath, HANDLE process)
{
	char fullPath[MAX_PATH];
	GetFullPathName(dllPath, sizeof(fullPath), fullPath, nullptr);

	// alloc remote memory for string with DLL path
	size_t size = strlen(fullPath) + 1;
	void* remotemem = VirtualAllocEx(process, nullptr, size, MEM_COMMIT, PAGE_READWRITE);
	if (remotemem == nullptr) throw "Couldn't allocate remote memory";

	// write DLL path to remote memory
	size_t written;
	if (!WriteProcessMemory(process, remotemem, fullPath, size, &written)) throw "Couldn't write remote memory";
	if (written != size) throw "Couldn't write full remote memory";

	// create remote thread with LoadLibraryA as starting function
	auto kernel = GetModuleHandle("Kernel32");
	HANDLE thread = CreateRemoteThread(process, nullptr, 0, (LPTHREAD_START_ROUTINE) GetProcAddress(kernel, "LoadLibraryA"), remotemem, 0, nullptr);
	WaitForSingleObject(thread, INFINITE);

	DWORD exitCode;
	GetExitCodeThread(thread, &exitCode); // LoadLibraryA returns the handle to the DLL
	CloseHandle(thread);
	VirtualFreeEx(process, remotemem, size, MEM_RELEASE); // release remote memory

	if (exitCode == 0)
	{
		throw "Could not load remote DLL";
	}
	else std::cerr << "DLL loaded" << std::endl;
}
void injectRemoteThreadCode(HANDLE process)
{
	// alloc remote memory for string with DLL path
	SSIZE_T size = ((char*) ThreadFund) - ((char*) ThreadFunc);
	std::cerr << "Size: " << size << std::endl;

	void* remotemem = VirtualAllocEx(process, nullptr, size, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
	if (remotemem == nullptr) throw "Couldn't allocate remote memory";

	// write DLL path to remote memory
	size_t written;
	if (!WriteProcessMemory(process, remotemem, ThreadFunc, size, &written)) throw "Couldn't write remote memory";
	if (written != size) throw "Couldn't write full remote memory";

	// create remote thread with LoadLibraryA as starting function
	auto kernel = GetModuleHandle("Kernel32");
	HANDLE thread = CreateRemoteThread(process, nullptr, 0, (LPTHREAD_START_ROUTINE) remotemem, remotemem, 0, nullptr);
	WaitForSingleObject(thread, INFINITE);

	DWORD exitCode;
	GetExitCodeThread(thread, &exitCode); // LoadLibraryA returns the handle to the DLL
	CloseHandle(thread);
	VirtualFreeEx(process, remotemem, size, MEM_RELEASE); // release remote memory

	std::cerr << "Ret: " << exitCode << std::endl;
}

std::vector<int> getProcessIds(const std::string& name)
{
	std::vector<int> ids;

	HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
	if (hSnapshot)
	{
		PROCESSENTRY32 pe32;
		pe32.dwSize = sizeof(PROCESSENTRY32);
		if (Process32First(hSnapshot, &pe32))
		{
			do {
				if (!strcmp(pe32.szExeFile, name.c_str()))
				{
					ids.push_back(pe32.th32ProcessID);
				}
			} while (Process32Next(hSnapshot, &pe32));
		}
		CloseHandle(hSnapshot);
	}

	return ids;
}

DWORD WINAPI pipeServer(void*)
{
	char buffer[1024];
	DWORD dwRead;

	HANDLE pipe = CreateNamedPipe(TEXT(PIPE_NAME),
		PIPE_ACCESS_DUPLEX | PIPE_TYPE_BYTE | PIPE_READMODE_BYTE,
		PIPE_WAIT,
		PIPE_UNLIMITED_INSTANCES,
		1024 * 16,
		1024 * 16,
		NMPWAIT_USE_DEFAULT_WAIT,
		nullptr);
	
	while (pipe != INVALID_HANDLE_VALUE)
	{
		if (ConnectNamedPipe(pipe, nullptr) != FALSE)
		{
			std::cerr << "Server connection start" << std::endl;
			while (ReadFile(pipe, buffer, sizeof(buffer) - 1, &dwRead, nullptr) != FALSE)
			{
				buffer[dwRead] = '\0';
				std::cerr << "Server received: " << buffer << std::endl;
			}
			std::cerr << "Server connection end" << std::endl;
		}

		DisconnectNamedPipe(pipe);
	}

	return 0;
}

int main(int argc, char** argv)
{
	try
	{
		if (argc < 2) throw "Not enough arguments";

		auto* processName = argv[1];
		auto* dllPath = argc > 2 ? argv[2] : nullptr;

		auto ids = getProcessIds(processName);
		if (ids.empty()) throw "Could not find process";

		HANDLE thread = CreateThread(nullptr, 0, pipeServer, nullptr, 0, nullptr);

		for (auto id : ids)
		{
			std::cerr << "Opening process " << processName << " (" << id << ")" << std::endl;
			HANDLE process = OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_CREATE_THREAD, false, id);
			if (process == nullptr) throw "Could not open process";

			injectRemoteThreadDll(dllPath, process);
			//injectRemoteThreadCode(process);
		}

		WaitForSingleObject(thread, INFINITE);
		CloseHandle(thread);
	}
	catch (const char* exc)
	{
		std::cerr << exc << std::endl;
		return 1;
	}

	return 0;
}
