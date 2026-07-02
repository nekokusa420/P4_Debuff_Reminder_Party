using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Automation;
using ECommons.ChatMethods;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Status = Lumina.Excel.Sheets.Status;
using UIColor = ECommons.ChatMethods.UIColor;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P4_Debuff_Reminder_Party : SplatoonScript<P4_Debuff_Reminder_Party.Config>
{
    public override Metadata Metadata { get; } = new(11, "NightmareXIV, mirage");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    private List<string> VfxLie = ["vfx/common/eff/z3oy_stlp6_c0c.avfx", "vfx/common/eff/z3oy_stlp4_c0c.avfx"];
    private List<string> VfxTruth = ["vfx/common/eff/z3oy_stlp7_c0c.avfx", "vfx/common/eff/z3oy_stlp5_c0c.avfx"];
    private record struct StatusInfo(uint objectId, uint statusId);
    private List<StatusInfo> FakeStatuses = [];

    public class Debuffs
    {
        public static uint[] DebuffDontMove = [5546, 1072, 1384, 2657, 3793, 3802, 4144];
        public static uint[] DebuffLookAway = [5543, 452];
        public static uint[] DebuffStack = [1023, 5545, 2142];
        public static uint[] DebuffSpread = [587, 3799, 5544];
        public static uint[] DebuffFireSpread = [1600, 5547];
        public static uint[] DebuffDonut = [1601, 5548];
        public static uint DebuffLive = 454;
        public static uint[] DebuffDie = [1382, 5464];
        public static uint[] DebuffWhitewould = [4887, 5541];
        public static uint[] DebuffBlackwound = [4888, 5542];
    }

    private Dictionary<uint, bool> IsTruth = [];
    public List<uint> DebuffList
    {
        get
        {
            if(field == null)
            {
                field = [];
                foreach(var x in typeof(Debuffs).GetFields().Select(x => x.GetValue(null)!))
                {
                    if(x is uint u) field.Add(u);
                    if(x is uint[] u2) field.AddRange(u2);
                }
            }
            return field;
        }
    }
    private List<uint> field;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Black", """{"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50069],"refActorComparisonType":6,"includeRotation":true}""");
        Controller.RegisterElementFromCode("White", """{"Name":"","type":3,"refY":40.0,"radius":12,"fillIntensity":0.6,"refActorNPCNameID":6055,"refActorRequireCast":true,"refActorCastId":[50068],"refActorComparisonType":6,"includeRotation":true}""");
        Controller.RegisterElementsFromMultilineCode("""
            {"Name":"LookAway","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278190335,"thicc":3.0,"overlayText":"LOOK AWAY","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true,"overlayVOffset":2.0}
            {"Name":"LookAt","type":1,"radius":0.0,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2550136832,"overlayTextColor":4278255376,"thicc":3.0,"overlayText":"LOOK AT","refActorName":"*","refActorRequireBuff":true,"refActorBuffId":[5543],"refActorUseBuffTime":true,"refActorBuffTimeMax":15.0,"tether":true,"overlayVOffset":2.0}
            {"Name":"EyeScope","type":4,"radius":15.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3355506687,"fillIntensity":0.125,"thicc":3.0,"refActorType":1,"includeRotation":true,"FillStep":99.0,"RenderEngineKind":2}
            {"Name":"Hint","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayTextColor":4292739327,"overlayVOffset":5.0,"thicc":0.0,"overlayText":"test","refActorType":1}
            {"Name":"StackSupport","refX":100.0,"refY":89.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Stack support","tether":true}
            {"Name":"StackDPS","refX":100.0,"refY":111.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"stack dps","tether":true}
            {"Name":"SpreadSupport","refX":89.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread support","tether":true}
            {"Name":"SpreadDPS","refX":111.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread dps","tether":true}
            {"Name":"StackSupport_2","refX":100.0,"refY":89.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Stack support","tether":true}
            {"Name":"StackDPS_2","refX":100.0,"refY":111.0,"radius":3.0,"Donut":0.5,"color":3355508521,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4280024832,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"stack dps","tether":true}
            {"Name":"SpreadSupport_2","refX":89.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread support","tether":true}
            {"Name":"SpreadDPS_2","refX":111.0,"refY":100.0,"radius":3.0,"Donut":0.5,"color":3355501823,"fillIntensity":0.5,"overlayBGColor":2650800128,"overlayTextColor":4278255605,"overlayVOffset":1.2,"thicc":4.0,"overlayText":"Spread dps","tether":true}
            {"Name":"MiddleGaze","refX":99.61274,"refY":99.88139,"refZ":-1.9073486E-06,"radius":4.0,"Donut":0.5,"fillIntensity":0.5,"overlayVOffset":1.2,"thicc":6.0,"tether":true}
            {"Name":"MiddleDrop","refX":99.61274,"refY":99.88139,"refZ":-1.9073486E-06,"radius":3.0,"Donut":0.5,"fillIntensity":0.5,"overlayTextColor":4278228223,"overlayVOffset":1.2,"thicc":6.0,"overlayText":"$ELEMENT","tether":true}
            """);
    }

    // 以下係修改後嘅主要部分（OnGainBuffEffect + OnSettingsDraw + Config）
    private void ShowSpread(float timer) { /* 原有不變 */ if(Controller.TryGetElementByName($"Spread{(BasePlayer.Job.IsDps() ? "DPS" : "Support")}{(C.DifferentiateFirstSecondStackSpread && !IsFirstStackSpread() ? "_2" : "")}", out var e)) { e.Enabled = true; e.color = Controller.AttentionColor; e.overlayText = Str_Spread($"{timer:F1}"); } }
    private void ShowStack(float timer) { /* 原有不變 */ if(Controller.TryGetElementByName($"Stack{(BasePlayer.Job.IsDps() ? "DPS" : "Support")}{(C.DifferentiateFirstSecondStackSpread && !IsFirstStackSpread() ? "_2" : "")}", out var e)) { e.Enabled = true; e.color = Controller.AttentionColor; e.overlayText = Str_Stack($"{timer:F1}"); } }

    public override void OnUpdate() { /* 保持原有 OnUpdate */ Controller.Hide(); /* ... (原有全部 OnUpdate code) ... */ }

    public override void OnGainBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        if(DebuffList.Contains(Status.StatusId) && sourceId.TryGetPlayer(out var pc))
        {
            if(IsLie) FakeStatuses.Add(new(sourceId, Status.StatusId));

            if(pc.AddressEquals(BasePlayer))
            {
                if((Debuffs.DebuffSpread.Contains(Status.StatusId) && !IsLie) || (Debuffs.DebuffStack.Contains(Status.StatusId) && IsLie))
                {
                    uint markingParam = 0;
                    bool isTankOrHeal = pc.ClassJob.ValueNullable?.Value.Role.EqualsAny((byte)1, (byte)4) == true;

                    if(Status.RemainingTime > 60f)
                        markingParam = isTankOrHeal ? C.MarkingParamTankHealLong : C.MarkingParamDPSLong;
                    else
                        markingParam = isTankOrHeal ? C.MarkingParamTankHealShort : C.MarkingParamDPSShort;

                    if(C.UsePartyMark && markingParam != 0)
                    {
                        if(GenericHelpers.IsScreenReady() && EzThrottler.Throttle("Marking", 900))
                        {
                            var markSlot = TextCommandParam.Get(markingParam).Param.GetText();
                            var cmd = $"/marking {markSlot} {pc.Name}";
                            UseCommand(cmd);
                        }
                    }

                    if(C.OutputInChat)
                        Print(UIColor.Orange, Status.RemainingTime > 60f ? C.LongSpread.Get() : C.ShortSpread.Get());
                }

                if(Debuffs.DebuffLookAway.Contains(Status.StatusId))
                {
                    if(C.OutputInChat)
                        Print(UIColor.Red, Status.RemainingTime > 65f ? (IsLie?C.LongGazeInv.Get():C.LongGaze.Get()) : (IsLie?C.ShortGazeInv.Get():C.ShortGaze.Get()));
                }

                if(Debuffs.DebuffDontMove.Contains(Status.StatusId))
                {
                    if(C.OutputInChat)
                        Print(UIColor.Yellow, IsLie ? C.AccelerationBombInv.Get() : C.AccelerationBomb.Get());
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Different positions for first and second stack/spread", ref C.DifferentiateFirstSecondStackSpread);
        if(C.DifferentiateFirstSecondStackSpread) ImGuiEx.TextWrapped(ImGuiColors.DalamudRed, "   Go to Registered elements and adjust positions of elements with \"_2\" prefix for second set of spreads/stacks!!!");

        ImGui.Checkbox("Output your debuffs into local chat (for you only)", ref C.OutputInChat);
        if(C.OutputInChat)
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.EnumCombo("Override chat channel", ref C.OverrideChatType);
            ImGui.Unindent();
        }

        ImGuiEx.Checkbox("Party Mark Spreads (dangerous)", ref C.UsePartyMark, enabled: C.UsePartyMark || ImGuiEx.Ctrl);
        ImGuiEx.Tooltip("Hold CTRL and click to enable\n坦補用 bind1/bind2 | DPS用 stop1/stop2");

        if(C.UsePartyMark)
        {
            DrawMarkingParam("Tank/Healer Short (bind1)", ref C.MarkingParamTankHealShort);
            DrawMarkingParam("Tank/Healer Long (bind2)", ref C.MarkingParamTankHealLong);
            DrawMarkingParam("DPS Short (stop1)", ref C.MarkingParamDPSShort);
            DrawMarkingParam("DPS Long (stop2)", ref C.MarkingParamDPSLong);
        }

        // 其餘 sliders 同文字設定...
        ImGui.SetNextItemWidth(150f); ImGuiEx.SliderFloat("Display stack/spread in advance", ref C.StackSpreadTH, 3, 20);
        // ... (其他原有設定) ...
    }

    public class Config
    {
        public float StackSpreadTH = 8.5f;
        public float MoveDontmoveTH = 8f;
        public float LookDontlookTH = 10f;
        public float DonutAOETH = 10f;
        public bool DifferentiateFirstSecondStackSpread = false;

        public uint MarkingParamTankHealShort;
        public uint MarkingParamTankHealLong;
        public uint MarkingParamDPSShort;
        public uint MarkingParamDPSLong;

        public bool UsePartyMark = false;
        public bool OutputInChat = true;
        public XivChatType OverrideChatType = XivChatType.None;

        public InternationalString AccelerationBomb = new(en: "Acceleration bomb on YOU (DON'T MOVE)", jp: "加速度　とまる");
        public InternationalString AccelerationBombInv = new(en: "Inverted acceleration bomb on YOU (MOVE)", jp: "加速度　うごく");
        public InternationalString LongGaze = new(en: "LONG GAZE on YOU (Look Away)", jp: "遅　視線　みない");
        public InternationalString LongGazeInv = new(en: "LONG GAZE on YOU (Look At)", jp: "遅　視線　みる");
        public InternationalString ShortGaze = new(en: "SHORT GAZE on YOU (Look Away)", jp: "早　視線　みない");
        public InternationalString ShortGazeInv = new(en: "SHORT GAZE on YOU (Look At)", jp: "早　視線　みる");
        public InternationalString LongSpread = new(en:"LONG SPREAD on YOU", jp: "遅　散開");
        public InternationalString ShortSpread = new(en:"SHORT SPREAD on YOU", jp: "早　散開");
    }

    private void DrawMarkingParam(string name, ref uint param) { /* 原有 DrawMarkingParam code */ }
    private uint[] ValidTextParams = [80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100, 102, 104, 476, 478, 480,];
}