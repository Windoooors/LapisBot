﻿using System.Text.RegularExpressions;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Lapis.ImageGenerators;
using Lapis.Settings;

namespace Lapis.Commands.GroupCommands;

public class SettingsCommand : GroupCommand
{
    public static SettingsCommand Instance;

    public SettingsCommand()
    {
        CommandHead = "settings";
        DirectCommandHead = "settings";
        Instance = this;
    }

    public bool GetValue(SettingsIdentifierPair identifierPair, long groupId)
    {
        var defaultValue = true;

        foreach (var category in SettingsItems.Categories)
        foreach (var itemsOfACommand in category.Items)
            if (itemsOfACommand.Identifier == identifierPair.PrimeIdentifier)
                foreach (var item in itemsOfACommand.Items)
                    if (item.Identifier == identifierPair.Identifier)
                        defaultValue = item.DefaultValue;

        return SettingsPool.GetValue(identifierPair.ToString(), groupId, defaultValue);
    }

    public override void Parse(CqGroupMessagePostContext source)
    {
        Program.Session.SendGroupMessageAsync(source.GroupId, [
            new CqReplyMsg(source.MessageId),
            new CqImageMsg("base64://" + new BotSettingsImageGenerator().Generate(source.GroupId,
                GetValue(new SettingsIdentifierPair("compress", "1"), source.GroupId)))
        ]);
    }

    public override void ParseWithArgument(string command, CqGroupMessagePostContext source)
    {
        if (!(source.Sender.Role is CqRole.Admin or CqRole.Owner ||
              source.Sender.UserId == BotConfiguration.Instance.AdministratorQqNumber))
        {
            Program.Session.SendGroupMessageAsync(source.GroupId,
                [new CqReplyMsg(source.MessageId), new CqTextMsg("您无权执行该指令")]);
            return;
        }

        var operationRegex = new Regex(".*?(\\.)*\\s(true|false)");

        if (!operationRegex.IsMatch(command))
        {
            Program.Session.SendGroupMessageAsync(source.GroupId, [
                new CqReplyMsg(source.MessageId), new CqTextMsg("参数输入有误")
            ]);
            return;
        }

        var identifiers = command.Split(" ")[0].Split(".");

        var identifierPair = new SettingsIdentifierPair(identifiers[0], identifiers[1]);

        var valueString = command.Split(" ")[1];
        var value = valueString.Equals("true");

        SettingsPool.SetValue(identifierPair.ToString(), source.GroupId, value);

        Program.Session.SendGroupMessageAsync(source.GroupId,
            [new CqReplyMsg(source.MessageId), new CqTextMsg("设置变更成功！")]);
    }
}