using ComputeShaderUtility;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;


public class Dispatcher : MonoBehaviour
{
    public struct Agent
    {
        public Vector2 position;
        public float angle;
    }

    ComputeBuffer agentBuffer;

    public ComputeShader moldCompute;

    RenderTexture diffusedTrailMap;
    RenderTexture trailMap;


    int texWidth = 1920;
    int texHeight = 1080;
    int agentNum = 1000000;

    int agentSpeed = 20;
    float trailStrength = 1.5f;
    float trailDecay = 0.01f;
    float trailDiffusion = 1f;
    float sensorAngleOffset = 45; // degrees
    float sensorDistance = 35;
    int sensorRadius = 1;
    float turnSpeed = 1.5f;

    int simSpeed = 10;

    void Start()
    {
        ComputeHelper.CreateRenderTexture(ref trailMap, texWidth, texHeight);
        ComputeHelper.CreateRenderTexture(ref diffusedTrailMap, texWidth, texHeight);


        Agent[] agents = new Agent[agentNum];

        for (int i = 0; i < agents.Length; i++)
        {
            Vector2 center = new Vector2(texWidth * 0.5f, texHeight * 0.5f);

            // Random
            //agents[i].position = new Vector2(Random.value * texWidth, Random.value * texHeight);
            // Center
            //agents[i].position = center;
            // disk
            agents[i].position = center + Random.insideUnitCircle * 0.50f * texHeight;
            // Circle
            //agents[i].position = center + Random.insideUnitCircle.normalized * 0.5f * texHeight;


            // Random
            //agents[i].angle = Random.value * Mathf.PI * 2;
            // Towards center
            agents[i].angle = Mathf.Atan2((center - agents[i].position).normalized.y, (center - agents[i].position).normalized.x);
            // X inwards, Y outwards
            //agents[i].angle = -Mathf.Atan2((center - agents[i].position).normalized.y, (center - agents[i].position).normalized.x);
            // Outwards
            //agents[i].angle = Mathf.PI + Mathf.Atan2((center - agents[i].position).normalized.y, (center - agents[i].position).normalized.x);
        }

        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentBuffer, agents, moldCompute, "agents", 0);
        moldCompute.SetInt("agentsLength", agentNum);
        moldCompute.SetInt("width", texWidth);
        moldCompute.SetInt("height", texHeight);
        moldCompute.SetInt("speed", agentSpeed); 
        moldCompute.SetFloat("trailStrength", trailStrength);
        moldCompute.SetFloat("decayRate", trailDecay);
        moldCompute.SetFloat("diffuseRate", trailDiffusion);
        moldCompute.SetFloat("sensorAngleOffset", sensorAngleOffset);
        moldCompute.SetFloat("sensorDistance", sensorDistance);
        moldCompute.SetInt("sensorRadius", sensorRadius);
        moldCompute.SetFloat("turnSpeed", turnSpeed);

        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = trailMap;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int i = 0; i < simSpeed; i++)
        {
            moldCompute.SetFloat("time", Time.time);
            moldCompute.SetFloat("deltaTime", Time.fixedDeltaTime);
            // simulate
            moldCompute.SetTexture(kernelIndex: 0, "trailMap", trailMap);
            ComputeHelper.Dispatch(moldCompute, agentNum, 1, 1, kernelIndex: 0);

            // Diffuse trail
            moldCompute.SetTexture(kernelIndex: 1, "trailMap", trailMap);
            moldCompute.SetTexture(kernelIndex: 1, "diffusedTrailMap", diffusedTrailMap);
            ComputeHelper.Dispatch(moldCompute, texWidth, texHeight, 1, kernelIndex: 1);

            // Apply blured trail
            ComputeHelper.CopyRenderTexture(diffusedTrailMap, trailMap);
        }
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), trailMap);
    }
}
