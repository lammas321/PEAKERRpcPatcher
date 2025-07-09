using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PEAKERRpcInfo
{
    public static class PEAKERRpcInfo
    {
        public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];

        public static void Patch(AssemblyDefinition assembly)
        {
            BepInEx.Logging.ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(nameof(PEAKERRpcInfo));

            if (!assembly.MainModule.TryGetTypeReference("Photon.Pun.PhotonMessageInfo", out TypeReference photonMessageInfoTypeRef))
            {
                logger.LogFatal("Photon.Pun.PhotonMessageInfo type not found.");
                return;
            }

            List<MethodDefinition> patchedMethods = [];

            foreach (TypeDefinition type in assembly.MainModule.GetTypes())
                foreach (MethodDefinition method in type.Methods)
                    try
                    {
                        if (
                            method.HasBody &&
                            method.HasCustomAttributes &&
                            method.CustomAttributes.Any(
                                attribute => attribute.AttributeType.ToString() == "Photon.Pun.PunRPC"
                            ) &&
                            !method.Parameters.Any(
                                parameter => parameter.ParameterType.ToString() == "Photon.Pun.PhotonMessageInfo"
                            )
                        )
                        {
                            method.Parameters.Add(new ParameterDefinition("info", ParameterAttributes.Optional, photonMessageInfoTypeRef));
                            logger.LogDebug($"Added Photon.Pun.PhotonMessageInfo info parameter onto {method.FullName}");

                            patchedMethods.Add(method);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"An exception occured adding to the parameters of {method.FullName}:\n{e}");
                    }

            foreach (TypeDefinition type in assembly.MainModule.GetTypes())
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method?.Body == null)
                        continue;

                    List<Instruction> patchedMethodCalls = [];

                    try
                    {
                        foreach (Instruction instruction in method.Body.Instructions)
                            foreach (MethodDefinition patchedMethod in patchedMethods)
                                if (instruction.MatchCallOrCallvirt(patchedMethod))
                                {
                                    patchedMethodCalls.Add(instruction);
                                    break;
                                }

                        if (patchedMethodCalls.Count == 0)
                            continue;

                        VariableDefinition photonMessageInfoVariable = new(photonMessageInfoTypeRef);
                        method.Body.Variables.Add(photonMessageInfoVariable);
                        logger.LogDebug($"Added Photon.Pun.PhotonMessageInfo variable to {method.FullName}");

                        method.Body.SimplifyMacros();
                        ILProcessor il = method.Body.GetILProcessor();

                        foreach (Instruction patchedMethodCall in patchedMethodCalls)
                        {
                            il.InsertBefore(patchedMethodCall, il.Create(OpCodes.Ldloca_S, photonMessageInfoVariable));
                            il.InsertBefore(patchedMethodCall, il.Create(OpCodes.Initobj, photonMessageInfoTypeRef));
                            il.InsertBefore(patchedMethodCall, il.Create(OpCodes.Ldloc_S, photonMessageInfoVariable));
                            logger.LogDebug($"Inserted default(Photon.Pun.PhotonMessageInfo) before call to {((MethodDefinition)patchedMethodCall.Operand).FullName}");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"An exception occured inserting default(Photon.Pun.PhotonMessageInfo) into {method.FullName}:\n{e}");
                    }
                    finally
                    {
                        if (patchedMethodCalls.Count != 0)
                            method.Body.OptimizeMacros();
                    }
                }
        }
    }
}