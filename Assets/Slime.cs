using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Slime : MonoBehaviour
{
    public int updatesPerFrame = 1;
    public int N_AGENTS = 10;
    public float moveSpeed = 100f;
    public float turnSpeed = 10f;
    public float sensorDistance = 10f;
    public int sensorSize = 2;
    public float sensorAngle = 45f;
    public int width = 512;
    private int height = 512;
    public struct Agent
    {
        public Vector2 position;
        public float angle;
    }
    private RenderTexture renderTexture;
    private RenderTexture fadedTexture;
    public ComputeShader computeShader;
    private ComputeBuffer agentBuffer;
    public float decayRate = 1f;
    public float diffuseRate = 1f;
    public List<Agent> agents = new List<Agent>();
    private void Start()
    {
        height = width / 16 * 9;
        CreateTexture(ref renderTexture);
        CreateTexture(ref fadedTexture);

        GetComponent<MeshRenderer>().material.mainTexture = renderTexture;
        for (int i = 0; i < N_AGENTS; i++)
        {
            Agent agent = new Agent();
            float angle = i*2*Mathf.PI/N_AGENTS;
            float distance = Mathf.Sqrt(Random.Range(0, 10000000)/10000000f)*width/6;
            agent.position = new Vector2(distance*Mathf.Cos(angle), distance*Mathf.Sin(angle))+new Vector2(width/2, height/2);
            agent.angle = angle+Mathf.PI;
            agents.Add(agent);
        }
        agentBuffer = new ComputeBuffer(agents.Count, sizeof(float) * 3);
        agentBuffer.SetData(agents);
        computeShader.SetBuffer(0, "agents", agentBuffer);
        computeShader.SetInt("numAgents", agents.Count);
        UpdateSettings();

        computeShader.SetTexture(0, "Texture", renderTexture);
        computeShader.SetTexture(0, "FadedTexture", fadedTexture);
        computeShader.SetTexture(1, "Texture", renderTexture);
        computeShader.SetTexture(1, "FadedTexture", fadedTexture);

        computeShader.SetInt("width", renderTexture.width);
        computeShader.SetInt("height", renderTexture.height);
    }

    private void UpdateSettings()
    {
        computeShader.SetFloat("moveSpeed", moveSpeed);
        computeShader.SetFloat("turnSpeed", turnSpeed);
        computeShader.SetFloat("sensorDistance", sensorDistance);
        computeShader.SetInt("sensorSize", sensorSize);
        computeShader.SetFloat("sensorAngle", sensorAngle * Mathf.Deg2Rad);
    }

    private void CreateTexture(ref RenderTexture texture)
    {
        texture = new RenderTexture(width, height, 0);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.anisoLevel = 0;
        texture.autoGenerateMips = false;
        texture.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
    }

    private void ClearTexture(RenderTexture rt)
    {
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }

    void Update() {
        computeShader.SetFloat("decayRate", decayRate);
        computeShader.SetFloat("diffuseRate", diffuseRate);
    }

    private void FixedUpdate() {
        UpdateSettings();
        computeShader.SetFloat("deltaTime", Time.fixedDeltaTime/updatesPerFrame);
        computeShader.SetFloat("time", Time.time);
        for(int i = 0; i < updatesPerFrame; i++)
        {
            computeShader.Dispatch(0, Mathf.CeilToInt(agents.Count/1024f), 1, 1);
            computeShader.Dispatch(1, Mathf.CeilToInt(renderTexture.width/32f), Mathf.CeilToInt(renderTexture.height/32f), 1);
            //Graphics.Blit(fadedTexture, renderTexture);
            Graphics.CopyTexture(fadedTexture, renderTexture);
        }
        //computeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
    }
    void OnDestroy()
    {
        agentBuffer.Release();
        fadedTexture.Release();
        renderTexture.Release();
    }
}
