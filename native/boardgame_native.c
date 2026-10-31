#include "boardgame_native.h"

int32_t BRG_CALL brg_restock_needed(int32_t available_copies, int32_t target_copies)
{
    if (available_copies < 0 || target_copies < 0)
        return -1;
    return available_copies < target_copies ? target_copies - available_copies : 0;
}
