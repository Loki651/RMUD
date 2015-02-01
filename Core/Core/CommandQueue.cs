﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace RMUD
{
    public static partial class Core
    {
        private class PendingCommand
        {
            internal Actor Actor;
            internal String RawCommand;
            internal Action ProcessingCompleteCallback;
        }

        private static Mutex PendingCommandLock = new Mutex();
        private static LinkedList<PendingCommand> PendingCommands = new LinkedList<PendingCommand>();
        private static Thread CommandExecutionThread;
        private static Thread IndividualCommandThread;
        private static AutoResetEvent CommandReadyHandle = new AutoResetEvent(false);
        private static AutoResetEvent CommandFinishedHandle = new AutoResetEvent(false);
        private static PendingCommand NextCommand;

        //The client command handler can set this flag when it wants the command timeout to be ignored.
        public static bool CommandTimeoutEnabled = true;


        public static ParserCommandHandler ParserCommandHandler;
        public static CommandParser DefaultParser;

        public static void EnqueuActorCommand(Actor Actor, String RawCommand)
        {
            PendingCommandLock.WaitOne();
            PendingCommands.AddLast(new PendingCommand { Actor = Actor, RawCommand = RawCommand });
            PendingCommandLock.ReleaseMutex();
        }

        public static void EnqueuActorCommand(Actor Actor, String RawCommand, Action ProcessingCompleteCallback)
        {
            PendingCommandLock.WaitOne();
            PendingCommands.AddLast(new PendingCommand { Actor = Actor, RawCommand = RawCommand, ProcessingCompleteCallback = ProcessingCompleteCallback });
            PendingCommandLock.ReleaseMutex();
        }

        internal static void DiscoverCommandFactories(StartUpAssembly In, CommandParser AddTo)
        {
            foreach (var type in In.Assembly.GetTypes())
                if (type.FullName.StartsWith(In.Info.BaseNameSpace) && type.IsSubclassOf(typeof(CommandFactory)))
                    CommandFactory.CreateCommandFactory(type).Create(AddTo);
        }

        private static void InitializeCommandProcessor()
        {
            DefaultParser = new CommandParser();

            foreach (var assembly in ModuleAssemblies)
                DiscoverCommandFactories(assembly, DefaultParser);

            ParserCommandHandler = new ParserCommandHandler(DefaultParser);
        }

        private static void StartCommandProcesor()
        {
            CommandExecutionThread = new Thread(ProcessCommands);
            CommandExecutionThread.Start();
        }

        private static void ProcessCommandsWorkerThread()
        {
            while (!ShuttingDown)
            {
                CommandReadyHandle.WaitOne();

                try
                {
                    //if (NextCommand.Actor.ConnectedClient != null)
                    //    NextCommand.Actor.ConnectedClient.TimeOfLastCommand = DateTime.Now;
                    NextCommand.Actor.CommandHandler.HandleCommand(NextCommand.Actor, NextCommand.RawCommand);
                }
                catch (System.Threading.ThreadAbortException)
                {
                    LogError("Command worker thread was aborted. Timeout hit?");
                    Core.ClearPendingMessages();
                }
                catch (Exception e)
                {
                    LogCommandError(e);
                    Core.ClearPendingMessages();
                }

                NextCommand = null;

                CommandFinishedHandle.Set();
            }
        }
                
        private static void ProcessCommands()
        {
            IndividualCommandThread = new Thread(ProcessCommandsWorkerThread);
            IndividualCommandThread.Start();

            while (!ShuttingDown)
            {
                System.Threading.Thread.Sleep(10);
                DatabaseLock.WaitOne();
                Heartbeat();
                DatabaseLock.ReleaseMutex();

                while (PendingCommands.Count > 0 && !ShuttingDown)
                {
                    PendingCommand PendingCommand = null;

                    PendingCommandLock.WaitOne();

                    try
                    {
                        PendingCommand = PendingCommands.FirstOrDefault(pc =>
                            {
                                return true;
                                //if (pc.Actor.ConnectedClient == null) return true;
                                //else return (DateTime.Now - pc.Actor.ConnectedClient.TimeOfLastCommand).TotalMilliseconds > SettingsObject.AllowedCommandRate;
                            });
                        if (PendingCommand != null)
                            PendingCommands.Remove(PendingCommand);
                    }
                    catch (Exception e)
                    {
                        LogCommandError(e);
                        PendingCommand = null;
                    }

                    PendingCommandLock.ReleaseMutex();

                    if (PendingCommand != null)
                    {
                        DatabaseLock.WaitOne();

                        NextCommand = PendingCommand;

                        //Reset flags that the last command may have changed
                        CommandTimeoutEnabled = true;
                        SilentFlag = false;
                        GlobalRules.LogRules(null);
                        
                        CommandReadyHandle.Set(); //Signal worker thread to proceed.
                        if (!CommandFinishedHandle.WaitOne(SettingsObject.CommandTimeOut))
                        {
                            if (!CommandTimeoutEnabled) //Timeout is disabled, go ahead and wait for infinity.
                                CommandFinishedHandle.WaitOne();
                            else
                            {
                                //Kill the command processor thread.
                                IndividualCommandThread.Abort();
                                ClearPendingMessages();
                                if (PendingCommand.Actor.ConnectedClient != null)
                                {
                                    PendingCommand.Actor.ConnectedClient.Send("Command timeout.\r\n");
                                    LogError(String.Format("Command timeout. {0} - {1}", PendingCommand.Actor.ConnectedClient.ConnectionDescription, PendingCommand.RawCommand));
                                }
                                else
                                    LogError(String.Format("Command timeout [No client] - {1}", PendingCommand.RawCommand));
                                IndividualCommandThread = new Thread(ProcessCommandsWorkerThread);
                                IndividualCommandThread.Start();
                            }
                        }

                        if (PendingCommand.ProcessingCompleteCallback != null)
                            PendingCommand.ProcessingCompleteCallback();
                                                
                        DatabaseLock.ReleaseMutex();
                    }
                }
            }

            IndividualCommandThread.Abort();
            if (Core.OnShutDown != null) Core.OnShutDown();
        }
    }
}