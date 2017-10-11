#include "registry.h"

std::unique_ptr<RegistryKey> RegistryKey::create(HKEY category, const std::string& key)
{
	HKEY handle;
	LONG result = RegCreateKeyEx(category, key.c_str(), 0, nullptr, REG_OPTION_NON_VOLATILE, KEY_WRITE | KEY_READ, nullptr, &handle, nullptr);
	if (result != ERROR_SUCCESS)
	{
		throw "Key could not be created";
	}

	return std::make_unique<RegistryKey>(handle);
}

bool RegistryKey::exists(HKEY category, const std::string& key)
{
	return RegistryKey(category, key, KEY_READ).isValid();
}

RegistryKey::RegistryKey(HKEY handle) : handle(handle), status(ERROR_SUCCESS)
{

}

RegistryKey::RegistryKey(HKEY category, const std::string& key, REGSAM access)
{
	this->status = RegOpenKeyEx(category, key.c_str(), 0, access, &this->handle);
}

RegistryKey::~RegistryKey()
{
	if (this->isValid())
	{
		RegCloseKey(this->handle);
	}
}

bool RegistryKey::isValid() const
{
	return this->status == ERROR_SUCCESS;
}

std::string RegistryKey::readString(const std::string& name)
{
	char buffer[512];
	DWORD bufferSize = sizeof(buffer);
	ULONG error = RegQueryValueEx(this->handle, name.c_str(), nullptr, nullptr, (LPBYTE)buffer, &bufferSize);
	if (ERROR_SUCCESS == error)
	{
		buffer[bufferSize] = '\0';
		return std::string(buffer);
	}
	else throw "Key not found";
}

void RegistryKey::writeString(const std::string& name, const std::string& value)
{
	ULONG error = RegSetValueEx(this->handle, name.c_str(), 0, REG_SZ, (LPBYTE)value.c_str(), static_cast<DWORD>(value.size() + 1));
	if (error != ERROR_SUCCESS)
	{
		throw "Key could not be written";
	}
}
