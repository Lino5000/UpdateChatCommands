using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Reflection;

namespace ChatCommands.Util
{
    /// <summary>
    /// Uses disgusting Reflection shenanigans to emulate the old behaviour of `ConsoleCommands.Trigger`
    /// </summary>
    internal class CommandInvoker
    {
        private object? commandManager;
        private MethodInfo? getCommandMethod;
        private PropertyInfo? callbackPropInfo;
        private MethodInfo? cmdInvokeMethod;
        private IMonitor monitor;

        public CommandInvoker(IModHelper helper, IMonitor monitor)
        {
            this.monitor = monitor;
            ObtainGetCommandMethod(helper);
        }

        private void ObtainGetCommandMethod(IModHelper helper)
        {
            FieldInfo? commandManagerFieldInfo = helper.ConsoleCommands.GetType().GetField("CommandManager", BindingFlags.NonPublic | BindingFlags.Instance);
            if (commandManagerFieldInfo == null)
            {
                this.monitor.Log($"Could not obtain CommandManager field info", LogLevel.Error);
                return;
            }
            //this.monitor.Log($"CommandManager has type {commandManagerFieldInfo.FieldType.FullName}", LogLevel.Debug);

            this.commandManager = commandManagerFieldInfo.GetValue(helper.ConsoleCommands);
            if (this.commandManager == null)
            {
                this.monitor.Log("Could not obtain ConsoleCommands object", LogLevel.Error);
                return;
            }
            //this.monitor.Log("Obtained ConsoleCommands object", LogLevel.Debug);

            this.getCommandMethod = commandManager.GetType().GetMethod("Get");
            if (this.getCommandMethod == null)
            {
                this.monitor.Log("Could not obtain ConsoleCommands.Get method", LogLevel.Error);
                return;
            }
            //this.monitor.Log("Obtained ConsoleCommands.Get MethodInfo", LogLevel.Debug);

            Type commandType = this.getCommandMethod.ReturnType;
            //this.monitor.Log($"Command type is {commandType.AssemblyQualifiedName}", LogLevel.Debug);

            PropertyInfo[] propertyInfos = commandType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name == "Callback")
                {
                    this.callbackPropInfo = propertyInfo;
                }
            }

            if (this.callbackPropInfo == null)
            {
                this.monitor.Log($"Could not obtain Command.Callback PropertyInfo", LogLevel.Error);
                return;
            }
            //this.monitor.Log("Obtained Command.Callback PropertyInfo", LogLevel.Debug);
            //this.monitor.Log($"Command.Callback type is {this.callbackPropInfo.PropertyType}", LogLevel.Debug);

            this.cmdInvokeMethod = this.callbackPropInfo.PropertyType.GetMethod("Invoke");
            if (this.cmdInvokeMethod == null)
            {
                this.monitor.Log($"Could not obtain Action.Invoke MethodInfo", LogLevel.Error);
                return;
            }
            //this.monitor.Log("Obtained Action.Invoke MethodInfo", LogLevel.Debug);
        }

        /// <summary>
        /// Invokes the requested console command.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="args">The command aarguments.</param>
        /// <returns>Returns whether the command was actually triggered; We may not have found the `CommandManager` properly, or the requested command may not exist.</returns>
        public bool InvokeCommand(string name, string[] args)
        {
            object[] getArguments = { name };
            object? command = this.getCommandMethod?.Invoke(this.commandManager, getArguments);
            if (command == null)
            {
                this.monitor.Log($"Could not obtain `{name}` command", LogLevel.Error);
                return false;
            }

            object? callback = this.callbackPropInfo?.GetValue(command);
            if (callback == null)
            {
                this.monitor.Log($"Could not obtain Callback value", LogLevel.Error);
                return false;
            }
            //this.monitor.Log($"Obtained callback for command `{name}`, calling with args `{args}`", LogLevel.Debug);

            if (this.cmdInvokeMethod == null)
                return false;

            object[] callbackArgs = { name, args };
            this.cmdInvokeMethod.Invoke(callback, callbackArgs);
            return true;
        }
    }
}
