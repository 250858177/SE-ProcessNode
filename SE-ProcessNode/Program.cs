using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //用来搜索包含字符串的程序块（集群处理）
        string likeName = "node";
        //单个节点控制上限
        int limit = 25;
        //是否是控制中心
        bool isCenter = false;

        Communication MSG;
        List<IMyProgrammableBlock> nodes = new List<IMyProgrammableBlock>();
        List<string> allPB = new List<string>();
        List<int> allPBCount = new List<int>();
        string data;
        public static IMyTextSurface textSurface;
        public static void print(string s)
        {
            textSurface?.WriteText(s + '\n', true);
        }
        public Program()
        {
            textSurface = Me.GetSurface(0);
            data = Me.CustomData;
            if (isCenter)
            {
                GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(nodes, g => g.CustomName.Contains(likeName));
                if (nodes.Count == 0)
                {
                    Echo("最少需要一个处理节点");
                    return;
                }
                nodes.ForEach(g => {
                    print(g.CustomData);
                    if (g.CustomData != null && g.CustomData != "")
                    {
                        allPB.Add(g.CustomData);
                        allPBCount.Add(g.CustomData.Split(Communication.objectDelimiter).Length);
                        print(allPB[allPB.Count - 1]);
                        print(allPBCount[allPBCount.Count - 1] + "");
                    }
                    else
                    {
                        allPB.Add("");
                        allPBCount.Add(0);
                        print("无");
                        print("0");
                    }
                });
            }
            MSG = new Communication(Me, IGC);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            print("初始化完成");
        }

        public void Save()
        {
            Me.CustomData = data;
        }

        string ssr = "";
        public void Main(string argument, UpdateType updateSource)
        {
            if (isCenter)
            {
                //处理中心逻辑
                textSurface?.WriteText("数据集散中心\n", false);
                if (nodes.Count == 0)
                {
                    Echo("最少需要一个处理节点");
                    print("最少需要一个处理节点");
                    return;
                }
                int iss = 0;
                print($"节点总数:{nodes.Count}");
                if (MSG.hasNewShip)
                {
                    List<MyIGCMessage> messages = MSG.getNewShipMessage();
                    print($"待处理消息:{messages.Count}");
                    foreach (MyIGCMessage message in messages)
                    {
                        print($"消息:{message.Data.ToString()}");
                        bool extend = false;
                        int index = 0;
                        for (int i = 0; i < allPB.Count; i++)
                        {
                            if (allPBCount[i] != 0 && allPB[i] != "" && allPB[i].Contains($"{message.Source}{Communication.idDelimiter}"))
                            {
                                extend = true;
                                index = i;
                                break;
                            }
                        }
                        print($"是否存在:{extend}");
                        if (extend)
                        {
                            //如果存在
                            Ship iShip = new Ship();
                            iShip.controlId = Me.EntityId;
                            iShip.id = message.Source;
                            iShip.action = "ACTIVE";
                            MSG.sendMessage(iShip.encode(), iShip.id);
                        }
                        else
                        {
                            //如果不存在
                            for (int i = 0; i < allPBCount.Count; i++)
                            {
                                print($"节点{i},总数:{allPBCount[i]}");
                                if (allPBCount[i] < limit)
                                {
                                    Ship ship = new Ship();
                                    ship.decode(message.Data.ToString());
                                    ship.id = message.Source;
                                    ship.controlId = Me.EntityId;
                                    ship.action = "ACTIVE";
                                    MSG.sendMessage(ship.encode(), ship.id);
                                    if (nodes[i].CustomData == null || nodes[i].CustomData == "")
                                    {
                                        nodes[i].CustomData += ship.encode();
                                    }
                                    else
                                    {
                                        nodes[i].CustomData += Communication.objectDelimiter + ship.encode();
                                    }
                                    ssr= ship.encode();
                                    allPBCount[i] += 1;
                                    allPB[i] = nodes[i].CustomData;
                                    break;
                                }
                            }
                        }
                        iss++;
                    }
                }
                print("实时访问监测:" + iss);
                print("最后一次信息编码:"+ssr);
                for (int i = 0; i < nodes.Count; i++)
                {
                    print($"节点{i}   对象总数:{allPBCount[i]}");
                }

            }
            else
            {
                //节点处理程序
                textSurface?.WriteText("数据转发中心\n", false);
                List<Ship> ships = Ship.decodeList(Me.CustomData);
                print("总数:" + ships.Count);
                foreach (Ship ship in ships)
                {
                    MSG.sendMessage(ship.encode(), ship.id);
                }

            }










        }
    }
}