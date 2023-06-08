#pragma once

#include <cstdio>
#include <cstdlib>

extern "C" {
#include <libavutil/log.h>
}

#define DllExport __declspec(dllexport)

// Ripped from https://github.com/lordmulder/asprintf-msvc
char* vasprintf(const char* const fmt, const va_list ap)
{
    char* buffer;

    const int str_len = _vscprintf(fmt, ap);
    if (str_len < 1)
    {
        return nullptr;
    }

    if (!(buffer = static_cast<char*>(malloc(sizeof(char) * (static_cast<size_t>(str_len) + 1U)))))
    {
        return nullptr;
    }

    if (_vsnprintf_s(buffer, static_cast<size_t>(str_len) + 1U, static_cast<size_t>(str_len) + 1U, fmt, ap) < 1)
    {
        free(buffer);
        buffer = nullptr;
    }

    return buffer;
}

typedef void (*ManagedAVLogCallback)(int, const char*);

typedef void (*AVLogCallback)(void*, int, const char*, va_list);

static ManagedAVLogCallback ManagedAVLogCallbackSingleton;

extern "C" {
inline DllExport void AVLogSetCallback(const ManagedAVLogCallback callback)
{
    ManagedAVLogCallbackSingleton = callback;
    av_log_set_callback([](void*, const int level, const char* fmt, const va_list va)
    {
        auto* message = vasprintf(fmt, va); // NOLINT(clang-diagnostic-format-nonliteral)
        ManagedAVLogCallbackSingleton(level, message);
        free(message);
    });
}

inline DllExport void AVLogUseDefaultCallback()
{
    av_log_set_callback(av_log_default_callback);
}
}
