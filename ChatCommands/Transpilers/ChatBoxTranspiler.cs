using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;

public static class ChatBoxTranspiler
{
    static IMonitor _monitor;

    public static void ApplyPatches(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(
            original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.receiveChatMessage)),
            transpiler: new HarmonyMethod(typeof(ChatBoxTranspiler), nameof(receiveChatMessage_AvoidAddingDuplicate_Patch))
        );
    }

    static IEnumerable<CodeInstruction> receiveChatMessage_AvoidAddingDuplicate_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var startIndex = -1;
        var endIndex = -1;

        MethodInfo parseMessageForEmojiInfo = AccessTools.Method(typeof(ChatMessage), "parseMessageForEmoji");
        MethodInfo listRemoveAtInfo = AccessTools.Method(typeof(List<ChatMessage>), "RemoveAt");

        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Callvirt)
            {
                // Find call to `parseMessageForEmoji` and set `startIndex` to the next instruction
                if (codes[i].OperandIs(parseMessageForEmojiInfo))
                {
                    _monitor.Log($"found `parseMessageForEmoji` at codes[{i}]", LogLevel.Trace);
                    startIndex = i+1;
                }
                // Find call to `List::RemoveAt` and set `endIndex` to that instruction
                if (codes[i].OperandIs(listRemoveAtInfo))
                {
                    _monitor.Log($"found `List::RemoveAt` at codes[{i}]", LogLevel.Trace);
                    endIndex = i+1;
                }
            }
        }
        if (startIndex > -1 && endIndex > -1)
        {
            _monitor.Log($"removing opcodes from {startIndex} to {endIndex}", LogLevel.Trace);
            codes.RemoveRange(startIndex, endIndex - startIndex);
        }

        return codes.AsEnumerable();
    }
}
