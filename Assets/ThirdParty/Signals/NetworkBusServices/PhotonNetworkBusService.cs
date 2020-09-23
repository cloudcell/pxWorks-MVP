//-----------------------------------------------------------------
// Example class of implementation INetworkBusService for Photon.
//-----------------------------------------------------------------
//
//
//#if PHOTON_UNITY_NETWORKING
//
//using ExitGames.Client.Photon;
//using Photon.Pun;
//using Photon.Realtime;
//using System;
//
//namespace SignalLayer
//{
//    /// <summary>
//    /// Implements INetworkBusService for Photon.
//    /// Requires installed Photon.Pun.
//    /// 
//    /// Just drag this script on some GameObject. It will automatically enable Networked Signals.
//    /// </summary>
//    class PhotonNetworkBusService : MonoBehaviourPunCallbacks, IOnSignalCallback, INetworkBusService
//    {
//        public event Action<int, byte[]> OnSignalCallback;

//        void Awake()
//        {
//            BusToNetworkBridge.Init<Bus>(this);
//        }

//        public void RaiseSignal(int signalId, byte[] data)
//        {
//            byte evCode = (byte)signalId;
//            RaiseSignalOptions raiseSignalOptions = new RaiseSignalOptions { Receivers = ReceiverGroup.Others };
//            SendOptions sendOptions = new SendOptions { Reliability = true };
//            PhotonNetwork.RaiseSignal(evCode, data, raiseSignalOptions, sendOptions);
//        }

//        public void OnSignal(SignalData photonSignal)
//        {
//            if (photonSignal.CustomData == null || photonSignal.CustomData is byte[])
//                OnSignalCallback?.Invoke(photonSignal.Code, (byte[])photonSignal.CustomData);
//        }
//    }
//}
//#endif