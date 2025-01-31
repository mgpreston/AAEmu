﻿using AAEmu.Game.Models.Game.Quests.Templates;

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace AAEmu.Game.Models.Game.Quests.Acts;

/// <summary>
/// Not used
/// </summary>
/// <param name="parentComponent"></param>
public class QuestActObjSendMail(QuestComponentTemplate parentComponent) : QuestActTemplate(parentComponent)
{
    public uint ItemId1 { get; set; }
    public int Count1 { get; set; }
    public uint ItemId2 { get; set; }
    public int Count2 { get; set; }
    public uint ItemId3 { get; set; }
    public int Count3 { get; set; }

    public bool UseAlias { get; set; }
    public uint QuestActObjAliasId { get; set; }

    public override bool RunAct(Quest quest, QuestAct questAct, int currentObjectiveCount)
    {
        return base.RunAct(quest, questAct, currentObjectiveCount);
    }
}
