﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EleCho.GoCqHttpSdk.Post;
using Lapis.Commands;
using Lapis.Commands.GroupCommands;
using Lapis.Commands.UniversalCommands;
using Lapis.Settings;
using Microsoft.Extensions.Logging;

namespace Lapis;

public class CommandParser
{
    private readonly Regex _headCommandRegex =
        new(@"(^lps\s|^六盘水\s|^l\s|^拉\s|^老婆说\s|^Lapis\s|^lapis\s|^lsp\s)");

    public void StartParsing(CqMessagePostContext source)
    {
        try
        {
            var commandString = source.Message.Text;

            RespondWithoutParsingCommand(source, commandString, Program.Commands);

            switch (source)
            {
                case CqGroupMessagePostContext groupSource:
                    if (SettingsCommand.Instance.GetValue(new SettingsIdentifierPair("litecommand", "1"),
                            groupSource.GroupId))
                        ParseDirectly(source, commandString, Program.Commands);
                    break;
                case CqPrivateMessagePostContext:
                    ParseDirectly(source, commandString, Program.Commands);
                    break;
            }

            if (!_headCommandRegex.IsMatch(commandString))
                return;

            commandString = _headCommandRegex.Replace(commandString, string.Empty, 1);

            if (!Parse(source, commandString, Program.Commands))
                HelpCommand.Instance.Parse(source);
        }
        catch (Exception ex)
        {
            HelpCommand.Instance.UnexpectedErrorHelp(source);
            Program.Logger.LogError(ex, ex.Message);
        }
    }

    private void RespondWithoutParsingCommand(CqMessagePostContext source, string commandString,
        Command[] commands)
    {
        foreach (var command in commands)
        {
            StartRespondWithoutParsingCommandTask(command, source, commandString);

            if (command.SubCommands.Length != 0)
                RespondWithoutParsingCommand(source, commandString, command.SubCommands);
        }
    }

    private bool Parse(CqMessagePostContext source, string commandString, Command[] commands)
    {
        var parsed = false;
        foreach (var command in commands)
            if (command.CommandHead != null)
            {
                var commandHeadMatchingEndingSpace = new Regex(@$"^({command.CommandHead})\s");
                if (commandHeadMatchingEndingSpace.IsMatch(commandString))
                {
                    commandString = commandHeadMatchingEndingSpace.Replace(commandString, string.Empty, 1);

                    if (command.SubCommands.Length != 0)
                        parsed = Parse(source, commandString, command.SubCommands);

                    if (parsed)
                        return true;

                    return StartParsingWithArgumentTask(command, source, commandString);
                }

                var commandHeadMatchingEndOfString = new Regex($"^({command.CommandHead})$");
                if (commandHeadMatchingEndOfString.IsMatch(commandString))
                {
                    if (command.SubCommands.Length != 0)
                        parsed = Parse(source, commandString, command.SubCommands);

                    if (parsed)
                        return true;

                    return StartParsingTask(command, source);
                }
            }
            else
            {
                if (command.SubCommands.Length == 0)
                    continue;

                parsed = Parse(source, commandString, command.SubCommands);
                if (parsed)
                    return true;
            }

        return false;
    }

    private void ParseDirectly(CqMessagePostContext source, string commandString,
        Command[] commands)
    {
        foreach (var command in commands)
        {
            if (command.SubCommands.Length != 0)
                ParseDirectly(source, commandString, command.SubCommands);

            if (command.DirectCommandHead == null)
                continue;

            var directCommandHeadMatchingEndingSpace = new Regex(@$"^({command.DirectCommandHead})\s");

            if (directCommandHeadMatchingEndingSpace.IsMatch(commandString))
            {
                commandString = directCommandHeadMatchingEndingSpace.Replace(commandString, string.Empty, 1);

                StartParsingWithArgumentTask(command, source, commandString);
                return;
            }

            var directCommandHeadMatchingEndOfString = new Regex($"^({command.DirectCommandHead})$");

            if (directCommandHeadMatchingEndOfString.IsMatch(commandString))
            {
                StartParsingTask(command, source);
                return;
            }
        }
    }

    private bool StartParsingWithArgumentTask(Command command, CqMessagePostContext source, string commandString)
    {
        Task taskParse = null;

        switch (command)
        {
            case GroupCommand groupCommand:
                if (source is CqGroupMessagePostContext groupSource)
                    if (SettingsCommand.Instance.GetValue(command.ActivationSettingsSettingsIdentifier,
                            groupSource.GroupId))
                        taskParse = new Task(() => groupCommand.ParseWithArgument(commandString, groupSource));

                break;
            case PrivateCommand privateCommand:
                if (source is CqPrivateMessagePostContext privateSource)
                    taskParse = new Task(() => privateCommand.ParseWithArgument(commandString, privateSource));

                break;
            case UniversalCommand universalCommand:
                taskParse = new Task(() => universalCommand.ParseWithArgument(commandString, source));
                break;
        }

        if (taskParse == null)
            return false;
        taskParse.Start();
        return true;
    }

    private bool StartParsingTask(Command command, CqMessagePostContext source)
    {
        Task taskParse = null;
        switch (command)
        {
            case GroupCommand groupCommand:
                if (source is CqGroupMessagePostContext groupSource)
                    if (SettingsCommand.Instance.GetValue(command.ActivationSettingsSettingsIdentifier,
                            groupSource.GroupId))
                        taskParse = new Task(() => groupCommand.Parse(groupSource));

                break;
            case PrivateCommand privateCommand:
                if (source is CqPrivateMessagePostContext privateSource)
                    taskParse = new Task(() => privateCommand.Parse(privateSource));

                break;
            case UniversalCommand universalCommand:
                taskParse = new Task(() => universalCommand.Parse(source));
                break;
        }

        if (taskParse == null)
            return false;
        taskParse.Start();
        return true;
    }

    private void StartRespondWithoutParsingCommandTask(Command command, CqMessagePostContext source,
        string commandString)
    {
        Task taskParse = null;
        switch (command)
        {
            case GroupCommand groupCommand:
                if (source is CqGroupMessagePostContext groupSource)
                    if (SettingsCommand.Instance.GetValue(command.ActivationSettingsSettingsIdentifier,
                            groupSource.GroupId))
                        taskParse = new Task(() =>
                            groupCommand.RespondWithoutParsingCommand(commandString, groupSource));

                break;
            case PrivateCommand privateCommand:
                if (source is CqPrivateMessagePostContext privateSource)
                    taskParse = new Task(() =>
                        privateCommand.RespondWithoutParsingCommand(commandString, privateSource));

                break;
            case UniversalCommand universalCommand:
                taskParse = new Task(() =>
                    universalCommand.RespondWithoutParsingCommand(commandString, source));

                break;
        }

        if (taskParse == null)
            return;
        taskParse.Start();
    }
}