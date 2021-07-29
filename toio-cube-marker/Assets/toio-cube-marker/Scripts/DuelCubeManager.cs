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


        public List<Cube> RealCubes { get { return realManager.connectedCubes; } }
        private List<Cube> SimCubes { get { return simManager.connectedCubes; } }
        private List<CubeHandle> RealHandles { get { return realManager.connectedHandles; } }
        private List<CubeHandle> SimHandles { get { return simManager.connectedHandles; } }

        private List<Cube> assignedCubes = new List<Cube>();
        private List<CubeHandle> assignedHandles = new List<CubeHandle>();
        public int assignedCount { get {return assignedCubes.Count;} }


        public int NumRealCubes { get {return RealCubes.Count;} }


        public bool RequestCubes(int num)
        {
            assignedCubes.Clear();
            assignedHandles.Clear();

            if (isReal)
            {
                var handles = RealHandles;
                if (handles.Count >= num)
                {
                    assignedCubes = RealCubes;
                    assignedHandles = handles;
                    return true;
                }
                else return false;
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
            assignedCubes.Clear();
            assignedHandles.Clear();
        }


        public Vector3Int GetPose(byte idx)
        {
            if (idx < assignedCount)
            {
                var cube = assignedCubes[idx];
                return new Vector3Int(cube.x, cube.y, cube.angle);
            }
            else return Vector3Int.zero;
        }

        public void Move(byte idx, int uL, int uR)
        {
            if (idx < assignedCount)
            {
                var translate = (uL + uR) / 2;
                var rotate = uL - uR;
                assignedHandles[idx].Update();
                var mv = assignedHandles[idx].Move(translate, rotate, durationMs:2550, order:Cube.ORDER_TYPE.Strong);
            }
            // else Debug.LogWarning("idx=" + idx + " >= cubes.Length=" + handles.Count);
        }

        public void MoveHome(Vector3Int[] homes)
        {
            for (int i=0; i < assignedCount; i++)
            {
                if (i >= homes.Length) break;
                var home = homes[i];
                var cube = assignedCubes[i];
                cube.TargetMove(home.x, home.y, home.z);
            }
        }
        public void StopMoveAll()
        {
            foreach (var cube in assignedCubes)
            {
                cube.Move(0 ,0, 0, order:Cube.ORDER_TYPE.Strong);
            }
        }

        private void SetHandleBorder(CubeHandle handle)
        {
            var margin = 5;
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
                cube.idCallback.RemoveListener("DuelCubeManager");
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.RemoveListener("DuelCubeManager");
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
            }
            // Set border
            foreach (var handle in SimHandles)
                SetHandleBorder(handle);

            assignedCubes = SimCubes;
            assignedHandles = SimHandles;

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
                cube.idCallback.RemoveListener("DuelCubeManager");
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.RemoveListener("DuelCubeManager");
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
            if (NumRealCubes >= 4) return RealCubes.ToArray();

            isRealConnecting = true;

            // Connect
            var cubes = await realManager.MultiConnect(num);

            // Set callbacks
            foreach (var cube in cubes)
            {
                cube.idCallback.RemoveListener("DuelCubeManager");
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.RemoveListener("DuelCubeManager");
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
            realManager.DisconnectAll();
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
            byte cubeIdx = (byte) this.assignedCubes.IndexOf(cube);
            idCallback?.Invoke(cubeIdx, cube.x, cube.y, cube.angle);
        }

        private Action<byte, uint> standardIDCallback = null;
        public void SetStandardIDCallback(Action<byte, uint> callback)
        {
            standardIDCallback = callback;
        }

        private void StandardIDCallback(Cube cube)
        {
            byte cubeIdx = (byte) this.assignedCubes.IndexOf(cube);
            standardIDCallback?.Invoke(cubeIdx, cube.standardId);
        }

        public void RequestCallback()
        {
            var cubes = assignedCubes;
            for (byte i = 0; i<cubes.Count; i++)
            {
                idCallback?.Invoke(i, cubes[i].x, cubes[i].y, cubes[i].angle);
                standardIDCallback?.Invoke(i, cubes[i].standardId);
            }
        }
        #endregion


    }

}
