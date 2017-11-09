#pragma once

#include <Windows.h>
#include <string>
#include <memory>

class RegistryKey
{
public:
	static std::unique_ptr<RegistryKey> create(HKEY category, const std::string& key);
	static bool exists(HKEY category, const std::string& key);

	explicit RegistryKey(HKEY handle);
	RegistryKey(HKEY category, const std::string& key, REGSAM access);
	~RegistryKey();

	RegistryKey(const RegistryKey& key) = delete;
	RegistryKey operator=(const RegistryKey& key) = delete;

	bool isValid() const;

	std::string readString(const std::string& name);
	void writeString(const std::string& name, const std::string& value);

private:
	LSTATUS status;
	HKEY handle;
};
