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
    //public ComputeShader moldRender;

    //RenderTexture displayTexture;
    RenderTexture diffusedTrailMap;
    RenderTexture trailMap;

    int texWidth = 1920;
    int texHeight = 1080;
    int agentNum = 10000;
    int agentSpeed = 500;
    float trailDecay = 0.4f;
    float traildiffusion = 1;

    void Start()
    {
        //ComputeHelper.CreateRenderTexture(ref displayTexture, texWidth, texHeight);
        ComputeHelper.CreateRenderTexture(ref trailMap, texWidth, texHeight);
        ComputeHelper.CreateRenderTexture(ref diffusedTrailMap, texWidth, texHeight);


        Agent[] agents = new Agent[agentNum];

        for (int i = 0; i < agents.Length; i++)
        {
            agents[i].position = new Vector2(texWidth / 2, texHeight / 2);
            agents[i].angle = Random.value * Mathf.PI * 2;
        }

        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentBuffer, agents, moldCompute, "agents", 0);
        moldCompute.SetInt("agentsLength", agentNum);
        moldCompute.SetInt("width", texWidth);
        moldCompute.SetInt("height", texHeight);
        moldCompute.SetInt("speed", agentSpeed);
        moldCompute.SetFloat("decayRate", trailDecay);
        moldCompute.SetFloat("diffuseRate", traildiffusion);

        //moldRender.SetBuffer(0, "agents", agentBuffer);
        //moldRender.SetInt("agentsLength", agentsNum);

        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = trailMap;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
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

    void LateUpdate()
    {
        //ComputeHelper.CopyRenderTexture(trailMap, displayTexture);

        //moldRender.SetTexture(0, "targetTexture", displayTexture);
        //ComputeHelper.Dispatch(moldRender,agentsNum, 1, 1, 0);
    }
}
