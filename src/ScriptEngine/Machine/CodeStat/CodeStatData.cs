﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/

namespace ScriptEngine.Machine
{
    public struct CodeStatData
    {
        public readonly CodeStatEntry Entry;
        public readonly long TimeElapsed;
        public readonly int ExecutionCount;

        public CodeStatData(CodeStatEntry entry, long time, int count)
        {
            Entry = entry;
            TimeElapsed = time;
            ExecutionCount = count;
        }
    }
}
