using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VoidForge.UI.Cyberdeck
{
    public sealed partial class CyberdeckHud
    {
        bool renaming;
        string editedName;
        int editedModule,systemRow;
        void Systems()
        {
            Box(sheet,28,177,690,477);
            Icon(sheet,"ui-module",58,200,150,100,White);
            Label(sheet,State.shipName,230,190,450,65,37);
            Click("module-rename","RENOMMER",230,265,450,65,()=>{editedModule=State.moduleId;editedName=State.shipName;renaming=true;Rebuild();},false,State.cockpitConnected,null,34);
            Label(sheet,"ÉNERGIE DISPONIBLE",58,350,610,44,30,Cyan);
            Label(sheet,$"{State.moduleEnergy:0.00} / {State.moduleCapacity:0.00} kWh",58,396,610,52,39);
            Label(sheet,$"PRODUCTION {State.moduleGeneration:0.0} kW   •   CHARGE {State.moduleLoad:0.0} kW",58,458,610,45,25,Cyan);
            Label(sheet,$"À VIDE : {State.moduleDryMass:0.0} kg",58,523,610,42,29);
            Label(sheet,$"CHARGÉ : {State.moduleDryMass+State.moduleCargoMass:0.0} kg",58,574,610,42,29);
            Box(sheet,738,177,1146,477);
            if(State.systems.Count==0)Empty("AUCUN SYSTÈME CONSTRUIT","ui-module",760,220,1070,360);
            systemRow=Mathf.Clamp(systemRow,0,Mathf.Max(0,State.systems.Count-4));
            for(int i=0;i<Mathf.Min(4,State.systems.Count-systemRow);i++)
            {
                var system=State.systems[systemRow+i];float y=190+i*104;
                Label(sheet,system.name,770,y,650,45,34);
                Label(sheet,system.status+(Mathf.Abs(system.kilowatts)>.001f?" • "+(system.kilowatts<0?"+":"−")+Mathf.Abs(system.kilowatts).ToString("0.##")+" kW":""),770,y+47,650,34,24,system.enabled?Cyan:Amber);
                Click("system-"+system.uid,system.enabled?"ON":"OFF",1490,y+8,285,73,()=>State.ToggleSystem(system.uid),system.enabled,State.cockpitConnected,"ui-power",35);
            }
            if(State.systems.Count>4)
            {
                Click("systems-up","↑",1785,187,85,75,()=>{systemRow--;Rebuild();},false,systemRow>0,null,36);
                Click("systems-down","↓",1785,553,85,75,()=>{systemRow++;Rebuild();},false,systemRow<State.systems.Count-4,null,36);
            }
        }
        void RenameKeyboard()
        {
            Box(sheet,200,165,1514,497,Ink,Cyan,16,4);
            Label(sheet,"NOM DU MODULE • "+editedName,235,177,1430,65,37);
            string[] rows={"AZERTYUIOP","QSDFGHJKLM","WXCVBN0123","456789-_.'"};
            for(int row=0;row<rows.Length;row++)for(int column=0;column<rows[row].Length;column++)
            {
                string key=rows[row][column].ToString();
                Click("rename-key-"+row+"-"+column,key,235+column*140,250+row*70,128,62,()=>{if(editedName.Length<32)editedName+=key;Rebuild();},false,null,null,34);
            }
            Click("rename-space","ESPACE",235,545,340,85,()=>{if(editedName.Length<32)editedName+=" ";Rebuild();},false,null,null,32);
            Click("rename-backspace","EFFACER",590,545,340,85,()=>{if(editedName.Length>0)editedName=editedName.Substring(0,editedName.Length-1);Rebuild();},false,null,null,32);
            Click("rename-cancel","ANNULER",945,545,340,85,()=>{renaming=false;Rebuild();},false,null,null,32);
            Click("rename-save","VALIDER",1300,545,340,85,()=>{renaming=false;State.Rename(editedModule,editedName);Rebuild();},true,!string.IsNullOrWhiteSpace(editedName),null,32);
        }
    }
}
