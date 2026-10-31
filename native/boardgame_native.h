#ifndef BOARDGAME_NATIVE_H
#define BOARDGAME_NATIVE_H

#include <stdint.h>

#ifdef _WIN32
#define BRG_EXPORT __declspec(dllexport)
#define BRG_CALL __cdecl
#else
#define BRG_EXPORT
#define BRG_CALL
#endif

/* Counts are int32_t, valid from 0 through INT32_MAX. -1 means invalid input. */
BRG_EXPORT int32_t BRG_CALL brg_restock_needed(int32_t available_copies, int32_t target_copies);

#endif
