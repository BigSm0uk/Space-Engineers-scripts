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
    List<IMyGyro> gyro;
    IMyBroadcastListener Input_signal;
    List<IMyRemoteControl> controller;
    MyIGCMessage message = new MyIGCMessage();
    List<IMyCameraBlock> cameras;
    List<IMyThrust> thrusters;
    List<IMyShipMergeBlock> merge;
    List<IMyGasTank> GasTanks;
    List<IMyTimerBlock> timers;

    IMyBlockGroup Rocket_System;

    Vector3D TargetXYZ, DirectionTargetXYZ , UpXYZ , TargetVelXYZ;
    
    VRage.MyTuple<Vector3D, Vector3D> myTuple = new VRage.MyTuple<Vector3D, Vector3D>();
    string tag_In = "228228", tag_OUT = "11111";
    int tick_count = 0, WHAM_coount = 0, go_count = 0;
    double ThrusterPower;
    bool launch = false, isEnabled_thrust = false, is_WHAM = false, stockpile = true;
   
    Program()
    {
        Input_signal = IGC.RegisterBroadcastListener(tag_In);
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }

    public void Main(string arg)
    {      
        if (tick_count++ % 120 == 0)
            Refresher();
            foreach (var i in GasTanks)
            {
                i.Stockpile = stockpile;
            }
        if (Input_signal.HasPendingMessage)
        {
            message = Input_signal.AcceptMessage();     
            try
            {
                myTuple = (VRage.MyTuple<Vector3D, Vector3D>)message.Data;
                if(myTuple.Item1 !=Vector3D.Zero )
                    TargetXYZ = myTuple.Item1;
                if (myTuple.Item2 != Vector3D.Zero)
                    TargetVelXYZ = myTuple.Item2;
            }    
            catch
            {
                stockpile = false;
                if (message.Data.ToString() == "start")
                {                   
                    launch = true;
                    isEnabled_thrust = true;
                    TorpedoVelCalc();
                }
                if (message.Data.ToString() == "WHAM")
                {
                    is_WHAM = true;
                    isEnabled_thrust = true;
                }
            }
        }
        if (isEnabled_thrust)
        {
            merge[0].Enabled = false;
            gyro[0].GyroOverride = true;
            foreach (var i in thrusters)
            {
                i.Enabled = true;
            }
            isEnabled_thrust = false;
        }
        if (is_WHAM && ++WHAM_coount > 60)
        {
            
            UpXYZ = -controller[0].GetNaturalGravity();
            WHAM(UpXYZ);
        }
        if (launch && !Vector3D.IsZero(TargetXYZ) && ++go_count >120)
        {
            foreach (var i in cameras)
            {
                MyDetectedEntityInfo myDetectedEntityInfo = i.Raycast(10000, 0, 0);
                if (myDetectedEntityInfo.Type == MyDetectedEntityType.LargeGrid)
                {
                    TargetXYZ = (Vector3D)myDetectedEntityInfo.HitPosition;
                    TargetVelXYZ = myDetectedEntityInfo.Velocity;
                    
                }
            }
            AIM();
        }
    }
    void AIM()
    {
        DirectionTargetXYZ = TorpedoVelCalc();
        Vector3D V3DLeft = controller[0].WorldMatrix.Left;
        Vector3D V3DUp = controller[0].WorldMatrix.Up;
        Vector3D V3DFow = controller[0].WorldMatrix.Forward;

        Vector3D TargetNorm = Vector3D.Normalize(DirectionTargetXYZ);
        double pitch = Math.Acos(Vector3D.Dot(V3DUp, Vector3D.Normalize(Vector3D.Reject(TargetNorm , V3DLeft)))) - (Math.PI / 2);
        double yaw = Math.Acos(Vector3D.Dot(V3DLeft, Vector3D.Normalize(Vector3D.Reject(TargetNorm, V3DUp)))) - (Math.PI / 2);
        double roll = Math.Acos(Vector3D.Dot(V3DLeft, Vector3D.Normalize(Vector3D.Reject(-controller[0].GetNaturalGravity(), V3DFow)))) - (Math.PI / 2);

        gyro[0].Yaw = (float)yaw * 5f;
        gyro[0].Pitch = (float)pitch * 5f;
        gyro[0].Roll = (float)roll;
    }
    void WHAM(Vector3D UpXYZ)
    {               
        Vector3D V3DLeft = controller[0].WorldMatrix.Left;
        Vector3D V3DUp = controller[0].WorldMatrix.Up;
        Vector3D V3DFow = controller[0].WorldMatrix.Forward;

        if (WHAM_coount <= 600)
            DirectionTargetXYZ = UpXYZ;
        else
        {
            Vector3D Target = TargetXYZ - controller[0].GetPosition();
            Vector3D TargProj = Vector3D.Normalize(controller[0].GetNaturalGravity()) * Vector3D.Dot(Target, Vector3D.Normalize(controller[0].GetNaturalGravity()));
            DirectionTargetXYZ = Target - TargProj;
            if (CalculateWham_Angle() < 0.26)
                DirectionTargetXYZ = Target;
        }
        
        Vector3D TargetNorm = Vector3D.Normalize(DirectionTargetXYZ);
        double pitch = Math.Acos(Vector3D.Dot(V3DUp, Vector3D.Normalize(Vector3D.Reject(TargetNorm, V3DLeft)))) - (Math.PI / 2);
        double yaw = Math.Acos(Vector3D.Dot(V3DLeft, Vector3D.Normalize(Vector3D.Reject(TargetNorm, V3DUp)))) - (Math.PI / 2);
        double roll = Math.Acos(Vector3D.Dot(V3DLeft, Vector3D.Normalize(Vector3D.Reject(-controller[0].GetNaturalGravity(), V3DFow)))) - (Math.PI / 2);

        gyro[0].Yaw = (float)yaw * 5f;
        gyro[0].Pitch = (float)pitch * 5f;
        gyro[0].Roll = (float)roll;
        Echo(CalculateWham_Angle().ToString());
    }
    void Refresher ()
    {
        merge = new List<IMyShipMergeBlock>();
        gyro = new List<IMyGyro>();
        controller = new List<IMyRemoteControl>();
        thrusters = new List<IMyThrust>();
        cameras = new List<IMyCameraBlock>();
        GasTanks = new List<IMyGasTank>();
        timers = new List<IMyTimerBlock>();
        Rocket_System = GridTerminalSystem.GetBlockGroupWithName("Rocket_System");
        Rocket_System.GetBlocksOfType<IMyRemoteControl>(controller);
        Rocket_System.GetBlocksOfType<IMyGyro>(gyro);
        Rocket_System.GetBlocksOfType<IMyTimerBlock>(timers);
        Rocket_System.GetBlocksOfType<IMyCameraBlock>(cameras);
        Rocket_System.GetBlocksOfType<IMyThrust>(thrusters);
        Rocket_System.GetBlocksOfType<IMyShipMergeBlock>(merge);
        Rocket_System.GetBlocksOfType<IMyGasTank>(GasTanks);
        Echo("Thrusters" + (thrusters.Count != 0 ? " here" : " fidnt"));
        Echo("Controller" + (controller.Count != 0 ? " here" : " fidnt")); 
        Echo("Timers" + (timers.Count != 0 ? " here" : " fidnt"));
        Echo("Hydro " + GasTanks.Count );
        Echo("Merge" + (merge.Count != 0 ? " here" : " fidnt"));
        Echo("Cameras count " + cameras.Count);
        if (cameras.Count != 0)
        {
            foreach (var i in cameras)
            {
                i.EnableRaycast = true;
            }
        }
        Echo("Gyro" + (gyro != null ? " here" : " fidnt"));
    }
    Vector3D TorpedoVelCalc()
    {
        DirectionTargetXYZ = TargetXYZ - controller[0].GetPosition();
        double orth = Vector3D.Dot(TargetVelXYZ, Vector3D.Normalize(DirectionTargetXYZ));
        Vector3D TangXYZ = Vector3D.Reject(TargetVelXYZ, Vector3D.Normalize(DirectionTargetXYZ));
        double scalarOrth = Math.Sqrt(controller[0].GetShipVelocities().LinearVelocity.Length() * controller[0].GetShipVelocities().LinearVelocity.Length() - TangXYZ.Length() * TangXYZ.Length());
        Vector3D TorpOrthXYZ = Vector3D.Normalize(DirectionTargetXYZ) * scalarOrth;
        return TorpOrthXYZ + TangXYZ;
    }
    double  CalculateWham_Angle ()
    {
        return Math.Acos(Vector3D.Dot(Vector3D.Normalize(controller[0].GetNaturalGravity()), Vector3D.Normalize(TargetXYZ - controller[0].GetPosition())));
    }
    Vector3D GravPreemptionCalc()
    {
        ThrusterPower = 0;
        foreach (var i in thrusters)
        {
            ThrusterPower += i.CurrentThrust;
        }
        Vector3D GravPreemptionXYZ = controller[0].GetNaturalGravity() * controller[0].CalculateShipMass().PhysicalMass;
        
        Vector3D ThrusterPowerXYZ = Math.Cos(Math.Asin(-(GravPreemptionXYZ.Length())/ ThrusterPower)) / Vector3D.Normalize(TorpedoVelCalc());
        return ThrusterPowerXYZ;
    }
}
