using System;
using System.Collections.Generic;
using UnityEngine;
using toio;
using Cysharp.Threading.Tasks;


namespace CubeMarker
{

    public class DuelCubeManager : MonoBehaviour
    {
        public GameObject CubeSimPrefab;

        #region Singleton
        private static DuelCubeManager ins = null;
        public static DuelCubeManager Ins { get {
            if (ins == null) ins = new DuelCubeManager();
            return ins;
        } }
        private DuelCubeManager() {}

        void Awake()
        {
            DuelCubeManager.ins = this;
            simManager = new CubeManager(ConnectType.Simulator);
            realManager = new CubeManager(ConnectType.Real);
        }
        #endregion

        public CubeManager simManager;
        public CubeManager realManager;
        private List<GameObject> simCubeObjs = new List<GameObject>();

        public bool isReal { get; set;} = false;
        public bool isConnecting { get {return isReal? isRealConnecting : isSimConnecting;} }
        public bool isConnected { get {return isReal? isRealConnected : isSimConnected;} }
        public bool isRealConnecting { get; protected set; } = false;
        public bool isRealConnected { get; protected set; } = false;
        public bool isSimConnecting { get; protected set; } = false;
        public bool isSimConnected { get; protected set; } = false;

        public Action<Cube, BLEPeripheralInterface> ConnectedCallback = null;
        public Action<Cube, BLEPeripheralInterface> DisconnectedCallback = null;


        public List<Cube> Cubes { get { return isReal? realManager.cubes : simManager.cubes; } }
        public List<Cube> RealCubes { get { return realManager.cubes; } }
        public List<Cube> SimCubes { get { return simManager.cubes; } }
        public List<CubeHandle> Handles { get { return isReal? realManager.handles : simManager.handles; } }
        public List<CubeHandle> RealHandles { get { return simManager.handles; } }
        public List<CubeHandle> SimHandles { get { return realManager.handles; } }


        public int NumRealCubes { get {return RealCubes.Count;} }
        public int NumCubes { get {return Cubes.Count;} }


        public bool RequestCubes(int num)
        {
            if (isReal)
            {
                return NumRealCubes >= num;
            }
            else
            {
                CreateConnectSimCubes(num);
                return true;
            }
        }

        public void ReleaseCubes()
        {
            StopMoveAll();
            SetIDCallback(null);
            SetStandardIDCallback(null);

            ClearSimCubes();
        }


        public Vector3Int GetPose(byte idx)
        {
            if (idx < NumCubes)
            {
                var cube = Cubes[idx];
                return new Vector3Int(cube.x, cube.y, cube.angle);
            }
            else return Vector3Int.zero;
        }

        public void Move(byte idx, int uL, int uR)
        {
            if (idx < NumCubes)
            {
                var translate = (uL + uR) / 2;
                var rotate = uL - uR;
                Handles[idx].Update();
                var mv = Handles[idx].Move(translate, rotate, durationMs:2550, order:Cube.ORDER_TYPE.Strong);
            }
            // else Debug.LogWarning("idx=" + idx + " >= cubes.Length=" + handles.Count);
        }

        public void MoveHome(Vector3Int[] homes)
        {
            for (int i=0; i < NumCubes; i++)
            {
                if (i >= homes.Length) break;
                var home = homes[i];
                var cube = Cubes[i];
                cube.TargetMove(home.x, home.y, home.z);
            }
        }
        public void StopMoveAll()
        {
            foreach (var cube in Cubes)
            {
                cube.Move(0 ,0, 0, order:Cube.ORDER_TYPE.Strong);
            }
        }

        private void SetHandleBorder(CubeHandle handle)
        {
            var margin = 11;
            handle.borderRect = new RectInt(98 + margin, 142 + margin, 304 - margin*2, 216 - margin*2);
        }


        #region Sim Connection
        public async void CreateConnectSimCubes(int num)
        {
            ClearSimCubes();
            isSimConnecting = true;

            // Instantiate
            for (int i=0; i<num; i++)
            {
                float x = - 0.06f + 0.06f * i;
                GameObject cubeObj = Instantiate(CubeSimPrefab, new Vector3(x, 0.001f, 0), Quaternion.identity);
                simCubeObjs.Add(cubeObj);
            }

            // Scan, Connect
            var cubes = await simManager.MultiConnect(num);
            // Set callbacks
            foreach (var cube in cubes)
            {
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
            }
            // Set border
            foreach (var handle in SimHandles)
                SetHandleBorder(handle);

            isSimConnecting = false;
            isSimConnected = true;
        }
        public void ClearSimCubes()
        {
            simManager.cubes.Clear();
            simManager.cubeTable.Clear();
            simManager.handles.Clear();
            simManager.navigators.Clear();
            foreach (var cubeObj in simCubeObjs) Destroy(cubeObj);
            simCubeObjs.Clear();
        }

        #endregion Sim Connection



        #region ====== Real Connection ======
        public async UniTask<Cube[]> SingleConnectRealCube()
        {
            if (NumRealCubes >= 4) return RealCubes.ToArray();

            isRealConnecting = true;

            // Connect
            var cube = await realManager.SingleConnect();
            // Set callbacks
            if (cube != null)
            {
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
            }
            // Set border
            foreach (var handle in RealHandles)
                SetHandleBorder(handle);

            isRealConnecting = false;

            isRealConnected = NumRealCubes > 0;

            return RealCubes.ToArray();
        }


        public async UniTask<Cube[]> MultiConnectRealCubes(int num = 4)
        {
            if (isRealConnected) return RealCubes.ToArray();

            isRealConnecting = true;

            // Connect
            var cubes = await realManager.MultiConnect(num);
            // Set callbacks
            foreach (var cube in cubes)
            {
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
            }
            // Set border
            foreach (var handle in RealHandles)
                SetHandleBorder(handle);

            isRealConnecting = false;

            isRealConnected = cubes.Length > 0;

            return RealCubes.ToArray();
        }

        public void DisconnectRealCubes()
        {
            foreach (var cube in RealCubes)
            isRealConnected = false;
        }

        #endregion ====== Real Connection ======



        #region Cube Callbak
        private Action<byte, int, int, int> idCallback = null;
        public void SetIDCallback(Action<byte, int, int, int> callback)
        {
            idCallback = callback;
        }

        private void IDCallback(Cube cube)
        {
            byte cubeIdx = (byte) this.Cubes.IndexOf(cube);
            idCallback?.Invoke(cubeIdx, cube.x, cube.y, cube.angle);
        }

        private Action<byte, uint> standardIDCallback = null;
        public void SetStandardIDCallback(Action<byte, uint> callback)
        {
            standardIDCallback = callback;
        }

        private void StandardIDCallback(Cube cube)
        {
            byte cubeIdx = (byte) this.Cubes.IndexOf(cube);
            standardIDCallback?.Invoke(cubeIdx, Cubes[cubeIdx].standardId);
        }

        public void RequestCallback()
        {
            for (byte i = 0; i<Cubes.Count; i++)
            {
                idCallback?.Invoke(i, Cubes[i].x, Cubes[i].y, Cubes[i].angle);
                standardIDCallback?.Invoke(i, Cubes[i].standardId);
            }
        }
        #endregion


    }

}
