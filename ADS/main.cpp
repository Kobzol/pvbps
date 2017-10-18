#include <iostream>
#include <cassert>

#include <Windows.h>

int main()
{
	const char* path = "test.txt:ads";
	const char* str = "hello";

	HANDLE file = CreateFile(path, GENERIC_WRITE, 0, nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);

	unsigned long len;
	WriteFile(file, str, std::strlen(str), &len, nullptr);
	CloseHandle(file);

	file = CreateFile(path, GENERIC_READ, 0, nullptr, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
	
	char buffer[1024];
	ReadFile(file, buffer, sizeof(buffer), &len, nullptr);
	buffer[len] = '\0';

	assert(strcmp(str, buffer) == 0);

	return 0;
}
