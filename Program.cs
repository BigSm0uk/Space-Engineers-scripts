using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
public sealed class Program : MyGridProgram
{
    IMyBlockGroup ScanerSystem;
    List<IMyMotorStator> Rotor = new List<IMyMotorStator>();
    List<IMyTextPanel> LCD = new List<IMyTextPanel>();
    List<IMyMotorStator> Hinge = new List<IMyMotorStator>();
    List<IMyMotorStator> AzElSystem = new List<IMyMotorStator>();
    List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>();
    List<IMyCockpit> Cockpit = new List<IMyCockpit>();
    List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
    IMyBroadcastListener Input_signal;
    MyIGCMessage message = new MyIGCMessage();
    MyDetectedEntityInfo myDetectedEntityInfo;
    VRage.MyTuple<Vector3D, Vector3D> myTuple = new VRage.MyTuple<Vector3D, Vector3D>();

    bool track = false;
    double Sx, h, t;
    Vector3D TargetXYZ;
    string tag_In1 = "228228", tag_In2 = "228229" , tag_OUT = "11111";
    Program()
    {
        ScanerSystem = GridTerminalSystem.GetBlockGroupWithName("ScanerSystem");
        ScanerSystem.GetBlocksOfType<IMyCockpit>(Cockpit);
        ScanerSystem.GetBlocksOfType<IMySoundBlock>(soundBlocks);
        ScanerSystem.GetBlocksOfType<IMyMotorStator>(AzElSystem);
        ScanerSystem.GetBlocksOfType<IMyTextPanel>(LCD);
        foreach (var i in AzElSystem)
        {
            if (i.CustomName.Contains("Rotor"))
                Rotor.Add(i);
            if (i.CustomName.Contains("Hinge"))
                Hinge.Add(i);
        }
        ScanerSystem.GetBlocksOfType<IMyCameraBlock>(Cameras);
        foreach (var i in Cameras)
        {
            i.EnableRaycast = true;
        }
        Input_signal = IGC.RegisterBroadcastListener(tag_In1);
        Input_signal = IGC.RegisterBroadcastListener(tag_In2);
        Echo("Rotor " + (Rotor.Count != 0 ? "here" : "fidnt"));
        Echo("Hinge " + (Hinge.Count != 0 ? "here" : "fidnt"));
        Echo("Cameras " + Cameras.Count);
        Echo("Cockpit " + (Cockpit.Count != 0 ? "here" : "fidnt"));
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }
    public void Main(string arg)
    {
        foreach(var i in Rotor)
            i.TargetVelocityRad = Cockpit[0].RotationIndicator.Y * 0.1f;
        foreach (var i in Hinge)
            i.TargetVelocityRad = -Cockpit[0].RotationIndicator.X * 0.1f;
        if(arg == "Detect")
        {
            Detect();
            
        }
        if (arg == "WHAM")
        {
            IGC.SendBroadcastMessage<string>(tag_In1, "WHAM");
        }
        if (arg == "start1")
        {
            IGC.SendBroadcastMessage<string>(tag_In1, "start");
        }
        if (arg == "start2")
        {
            IGC.SendBroadcastMessage<string>(tag_In2, "start");
        }
        if (arg == "Track start")
        {
            track = true;
        }
        if (arg == "Track stop")
        {
            track = false;
        }
        if (track)
        {
            Tracking_mode();
            
        }
    }
    void Detect()
    {
        foreach (var i in Cameras)
        {
            MyDetectedEntityInfo myDetectedEntityInfo_cameras = i.Raycast(10000, 0, 0);
            if (myDetectedEntityInfo_cameras.Type == MyDetectedEntityType.LargeGrid)
            {
                myDetectedEntityInfo = myDetectedEntityInfo_cameras;
                foreach (var j in soundBlocks)
                {
                    j.Play();
                }
                foreach (var j in LCD)
                {
                    j.WriteText("Large Grid " + myDetectedEntityInfo_cameras.Name);
                    j.WriteText("\nX: " + myDetectedEntityInfo_cameras.Position.X, true);
                    j.WriteText("\nY: " + myDetectedEntityInfo_cameras.Position.Y, true);
                    j.WriteText("\nZ: " + myDetectedEntityInfo_cameras.Position.Z, true);
                    j.WriteText("\nShip speed : " + myDetectedEntityInfo_cameras.Velocity.Length(), true);
                    j.WriteText("\n" + (myDetectedEntityInfo_cameras.Position + myDetectedEntityInfo_cameras.Velocity.Length() *((Cockpit[0].GetPosition() - TargetXYZ + 2.5) / Math.Abs(myDetectedEntityInfo_cameras.Velocity.Length() - 300d) - 5)), true);
                }
                myTuple = VRage.MyTuple.Create<Vector3D, Vector3D>((Vector3D)myDetectedEntityInfo.HitPosition, myDetectedEntityInfo.Velocity);
                IGC.SendBroadcastMessage<VRage.MyTuple<Vector3D, Vector3D>>(tag_In1, myTuple);
                IGC.SendBroadcastMessage<VRage.MyTuple<Vector3D, Vector3D>>(tag_In2, myTuple);
            }

        }
    }
    void Tracking_mode()
    {
        foreach (var i in Cameras)
        {
            MyDetectedEntityInfo myDetectedEntityInfo_cameras = i.Raycast(10000, 0, 0);
            if (myDetectedEntityInfo_cameras.Type == MyDetectedEntityType.LargeGrid)
            {
               myDetectedEntityInfo = myDetectedEntityInfo_cameras;
              myTuple = VRage.MyTuple.Create<Vector3D, Vector3D>(myDetectedEntityInfo.Position, myDetectedEntityInfo.Velocity);
                IGC.SendBroadcastMessage<VRage.MyTuple<Vector3D, Vector3D>>(tag_In1, myTuple);
                IGC.SendBroadcastMessage<VRage.MyTuple<Vector3D, Vector3D>>(tag_In2, myTuple);
            }            
        }
    }

}
