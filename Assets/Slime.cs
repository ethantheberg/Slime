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
    public float sensorAngle = 45f;
    public float trailStrength = 1f;
    public Gradient gradient;
    public float decayRate = 1f;
    public float diffuseRate = 1f;
    public int width = 512;
    private int height;
    public Material displayMaterial;
    public Material trailMapMaterial;
    public struct Agent
    {
        public Vector2 position;
        public float angle;
    }
    private RenderTexture trailMap;
    private RenderTexture fadedTrailMap;
    private RenderTexture displayTexture;
    private RenderTexture gradientTexture;
    public ComputeShader computeShader;
    private ComputeBuffer agentBuffer;
    public List<Agent> agents = new List<Agent>();
    private void Start()
    {
        height = width / 16 * 9;
        CreateTexture(ref trailMap, width, height, GraphicsFormat.R32_SFloat);
        CreateTexture(ref fadedTrailMap, width, height, GraphicsFormat.R32_SFloat);
        CreateTexture(ref displayTexture, width, height, GraphicsFormat.R8G8B8A8_UNorm);
        CreateTexture(ref gradientTexture, 256, 1, GraphicsFormat.R8G8B8A8_UNorm);
        Graphics.Blit(CreateGradientTexture(gradient), gradientTexture);

        trailMapMaterial.mainTexture = trailMap;
        displayMaterial.mainTexture = displayTexture;
        for (int i = 0; i < N_AGENTS; i++)
        {
            Agent agent = new Agent();
            float angle = i*2*Mathf.PI/N_AGENTS;
            float distance = Mathf.Sqrt(Random.Range(0, 10000000)/10000000f)*height/2*0.9f;
            agent.position = new Vector2(distance*Mathf.Cos(angle), distance*Mathf.Sin(angle))+new Vector2(width/2, height/2);
            agent.angle = angle+Mathf.PI/2;
            agents.Add(agent);
        }
        agentBuffer = new ComputeBuffer(agents.Count, sizeof(float) * 3);
        agentBuffer.SetData(agents);
        computeShader.SetBuffer(0, "agents", agentBuffer);
        computeShader.SetInt("numAgents", agents.Count);
        UpdateSettings();

        computeShader.SetTexture(0, "trailMap", trailMap);

        computeShader.SetTexture(1, "trailMap", trailMap);
        computeShader.SetTexture(1, "fadedTrailMap", fadedTrailMap);

        computeShader.SetTexture(2, "trailMap", trailMap);
        computeShader.SetTexture(2, "displayTexture", displayTexture);
        computeShader.SetTexture(2, "gradientTexture", gradientTexture);

        computeShader.SetInt("width", trailMap.width);
        computeShader.SetInt("height", trailMap.height);
    }

    private Texture2D CreateGradientTexture(Gradient gradient)
    {
        Texture2D texture = new Texture2D(256, 1);
        Color[] colors = new Color[256];
        for (int i = 0; i < 256; i++)
        {
            colors[i] = gradient.Evaluate(i/255f);
        }
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private void UpdateSettings()
    {
        Graphics.Blit(CreateGradientTexture(gradient), gradientTexture);
        computeShader.SetFloat("moveSpeed", moveSpeed);
        computeShader.SetFloat("turnSpeed", turnSpeed);
        computeShader.SetFloat("sensorDistance", sensorDistance);
        computeShader.SetFloat("trailStrength", trailStrength);
        computeShader.SetFloat("sensorAngle", sensorAngle * Mathf.Deg2Rad);
        computeShader.SetFloat("decayRate", decayRate);
        computeShader.SetFloat("diffuseRate", diffuseRate);
    }

    private void CreateTexture(ref RenderTexture texture, int w, int h, GraphicsFormat format)
    {
        texture = new RenderTexture(w, h, 0);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 0;
        texture.autoGenerateMips = false;
        texture.graphicsFormat = format;
    }

    private void ClearTexture(RenderTexture rt)
    {
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }

    private void FixedUpdate() {
        computeShader.SetFloat("deltaTime", Time.fixedDeltaTime/updatesPerFrame);
        computeShader.SetFloat("time", Time.time);
        bool mouseDown = Input.GetMouseButton(0);
        computeShader.SetBool("mouseDown", mouseDown);
        if(mouseDown){
            Vector2 mousePosition = new Vector2((Input.mousePosition.x/Screen.width)*width, (Input.mousePosition.y/Screen.height)*height);
            computeShader.SetVector("mousePosition", mousePosition);
        }
        for(int i = 0; i < updatesPerFrame; i++)
        {
            computeShader.Dispatch(0, Mathf.CeilToInt(agents.Count/1024f), 1, 1);
            computeShader.Dispatch(1, Mathf.CeilToInt(width/32f), Mathf.CeilToInt(height/32f), 1);
            Graphics.CopyTexture(fadedTrailMap, trailMap);
        }
        computeShader.Dispatch(2, Mathf.CeilToInt(width/32f), Mathf.CeilToInt(height/32f), 1);
    }
    void OnDestroy()
    {
        agentBuffer.Release();
        fadedTrailMap.Release();
        trailMap.Release();
    }
    private void OnValidate() {
        UpdateSettings();
    }
}
