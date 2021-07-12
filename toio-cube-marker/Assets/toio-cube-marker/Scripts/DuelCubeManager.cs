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
        }
        #endregion


        private List<GameObject> simCubeObjs = new List<GameObject>();
        private List<Cube> simCubes = new List<Cube>();
        private List<CubeHandle> simHandles = new List<CubeHandle>();
        private List<Cube> realCubes = new List<Cube>();
        private List<CubeHandle> realHandles = new List<CubeHandle>();


        public bool isReal { get; set;} = false;
        public bool isConnecting { get {return isReal? isRealConnecting : isSimConnecting;} }
        public bool isConnected { get {return isReal? isRealConnected : isSimConnected;} }
        public bool isRealConnecting { get; protected set; } = false;
        public bool isRealConnected { get; protected set; } = false;
        public bool isSimConnecting { get; protected set; } = false;
        public bool isSimConnected { get; protected set; } = false;

        public Action<Cube, BLEPeripheralInterface> ConnectedCallback = null;
        public Action<Cube, BLEPeripheralInterface> DisconnectedCallback = null;


        public List<Cube> Cubes { get {
            if (isReal) return realCubes;
            return simCubes;
        } }
        public List<CubeHandle> Handles { get {
            if (isReal) return realHandles;
            return simHandles;
        } }

        public Cube[] GetRealCubes()
        {
            return realCubes.ToArray();
        }

        public int NumRealCubes { get {return realCubes.Count;} }
        public int NumCubes { get {return Cubes.Count;} }


        public bool RequestCubes(int num)
        {
            if (isReal)
            {
                return realCubes.Count >= num;
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

            if (isReal) {}
            else
            {
                ClearSimCubes();
            }
        }


        public Vector3Int GetPose(byte idx)
        {
            var cubes = Cubes;
            if (idx < cubes.Count)
            {
                var cube = cubes[idx];
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
            var cubes = Cubes;
            for (int i=0; i<cubes.Count; i++)
            {
                if (i >= homes.Length) break;
                var home = homes[i];
                var cube = cubes[i];
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

        private CubeHandle CreateCubeHandle(Cube cube)
        {
            var h = new CubeHandle(cube);
            var margin = 11;
            h.borderRect = new RectInt(98 + margin, 142 + margin, 304 - margin*2, 216 - margin*2);
            return h;
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
            var peripherals = await new NearScanner(num, alwaysSim: true).Scan();
            var cubes = await new CubeConnecter(alwaysSim: true).Connect(peripherals);
            foreach (var cube in cubes)
            {
                simCubes.Add(cube);
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
                simHandles.Add(CreateCubeHandle(cube));
            }

            isSimConnecting = false;
            isSimConnected = true;
        }
        public void ClearSimCubes()
        {
            simCubes.Clear();
            foreach (var cubeObj in simCubeObjs) Destroy(cubeObj);
            simCubeObjs.Clear();
            simHandles.Clear();
        }

        #endregion Sim Connection



        #region ====== Real Connection ======
        public async UniTask<Cube[]> SingleConnectRealCube()
        {
            if (NumRealCubes >= 4) return realCubes.ToArray();

            isRealConnecting = true;

            var peri = await new NearestScanner().Scan();
            if (peri == null)
            {
                isRealConnecting = false;
                return realCubes.ToArray();
            }
            peri.AddConnectionListener("DuelCubeManager", this.OnPeripheralConnection);

            // Connect
            var cube = await new CubeConnecter().Connect(peri);
            if (cube != null)
            {
                realCubes.Add(cube);
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
                realHandles.Add(CreateCubeHandle(cube));
            }

            isRealConnecting = false;

            if (NumRealCubes > 0) isRealConnected = true;

            return realCubes.ToArray();
        }


        public async UniTask<Cube[]> MultiConnectRealCubes(int num = 4)
        {
            if (isRealConnected) return realCubes.ToArray();

            isRealConnecting = true;

            var peripherals = await new NearScanner(num).Scan();
            foreach (var peri in peripherals)
                peri.AddConnectionListener("DuelCubeManager", this.OnPeripheralConnection);

            // Connect
            var cubes = await new CubeConnecter().Connect(peripherals);
            foreach (var cube in cubes)
            {
                realCubes.Add(cube);
                cube.idCallback.AddListener("DuelCubeManager", IDCallback);
                cube.standardIdCallback.AddListener("DuelCubeManager", StandardIDCallback);
                realHandles.Add(CreateCubeHandle(cube));
            }

            isRealConnecting = false;

            if (cubes.Length > 0) isRealConnected = true;
            Debug.Log("Connect Over. cubes=" + realCubes.Count);

            return realCubes.ToArray();
        }

        public void DisconnectRealCubes()
        {
            foreach (var cube in realCubes)
            {
                var peri = (cube as CubeReal).peripheral;
                peri?.Disconnect();
            }
            realCubes.Clear();
            realHandles.Clear();
            isRealConnected = false;
        }

        private void OnPeripheralConnection(BLEPeripheralInterface peri)
        {
            Debug.Log("On peri connection: " + peri.device_address + "  " + peri.isConnected);
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
