using System;
using System.Collections.Generic;
using UnityEngine;


namespace CubeMarker
{

    public interface IUIGame
    {
        void Back();
        void SetDrawable(bool value);
        void Countdown(byte count, string zeroText="GO!", float duration=0.75f);
        void StartTimer(float timeLeft, float timeLimit);
        void StopTimer();
        void CountdownToQuit(byte sec);

        void CreateCubeMarker(int team, string name);
        void ClearCubeMarkers();
        void MoveCubeMarker(int idx, int matX, int matY, int matDeg);
        void SetCubeMarkerPen(int idx, Color color);
        void SetCubeMarkerPen(int idx, Texture2D tex);
        void SetCubeMarkerStatus(int idx, GameCubeStatus status);
        Dictionary<int, float> GetMarkerIdxRatioDict();
        byte[] GetOccupancyMap();
    }

    public interface IUIGameBattle
    {
        void ShowNStat(int num);
        void SetStat(int i, byte teamIdx, float ratio);

        void ShowResult(Dictionary<ActualPlayerID, float> pidRatioDict);
    }

    public interface IUIGameQuiz
    {
        Dictionary<string, string> GetLoadableTextures();
        void SetupQuiz(List<string> allNames, Dictionary<string, string> nameURLs, Dictionary<int, string> cubeIdxNames);
        void SetAnswerCallback(Action<string> callback);
        void SetAnswerable(bool value);
        void SetAnswerSlotAnswer(ActualPlayerID pid, string ans);
        void ShowResult(Dictionary<byte, string> teamIdxTexNameDict, Dictionary<ActualPlayerID, string> pidAnsDict, Dictionary<ActualPlayerID, float> pidTimeDict=null);
    }

}