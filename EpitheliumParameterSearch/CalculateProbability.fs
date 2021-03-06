﻿module CalculateProbability

//Abstracting the details of how everything is calculated
type technique = Simulation | Analysis | Hybrid

let analyticalScan allParameters = 
    let completeSet = Types.createParameterSet allParameters
    
    //Get numbers
    let probS = Parallel.arrayMap (fun input -> (AnalyticalCloneSizeDistribution.probabilityCloneSurvival input).Real) completeSet
    let probN = Parallel.arrayMap (fun input -> AnalyticalCloneSizeDistribution.probabilityCloneSizes input (List.init allParameters.maxN (fun i -> i+1)) allParameters.maxN) completeSet

    //Restructure results and put into a record
    Types.restructureParameterSet allParameters probS probN

let hybridScan allParameters = 
    let completeSet = Types.createParameterSet allParameters
    let probD = Parallel.arrayMap (fun input -> (AnalyticalCloneSizeDistribution.probabilityCloneDistribution input)) completeSet

    let probS = Array.map (fun (i: float []) -> 1. - i.[0]) probD
    let probN = Array.map (fun (i: float []) -> Array.init ((Array.length i)-1) (fun j -> i.[j+1] )  ) probD

    //Restructure results and put into a record
    Types.restructureParameterSet allParameters probS probN

let simulationScan allParameters number =
    //Simulations do not respect timepoints as input parameters
    let completeSet = Types.createParameterSet {allParameters with timePoints=[|0.<Types.week>|]}
    let probabilityDistributions = Parallel.arrayMap (fun input -> input 
                                                                    |> SimulationCloneSizeDistribution.parameterSetToClone (SimulationCloneSizeDistribution.Specified(List.ofArray allParameters.timePoints))
                                                                    |> SimulationCloneSizeDistribution.cloneProbability number
                                                                    ) completeSet
    //Investigate issue here
    let oneDLength = (Array.length probabilityDistributions) * (Array.length allParameters.timePoints)
    let convert i =  probabilityDistributions.[(i / (Array.length allParameters.timePoints))].[(i % (Array.length allParameters.timePoints))].basalFraction
    let probabilityDistributions1D = Array.init oneDLength (fun i -> convert i )
    //Todo probS is 1 - probabilityDistributions.[x].[0]
    let probS = Array.Parallel.map (fun (i: float []) -> 1. - i.[0]) probabilityDistributions1D
    //Todo probN is probabilityDistributions.[x][1..]
    let probN = Array.Parallel.map (fun (i: float []) -> Array.init ((Array.length i)-1) (fun j -> i.[j+1]) ) probabilityDistributions1D
    Types.restructureParameterSet allParameters probS probN


let parameterSearch (input : Types.parameterSearch) approach =
    match (approach,input.deltaRange) with 
    | (Simulation,_) -> simulationScan input 10000
    | (Analysis,Types.delta.Range(_)) -> failwith "Cannot generate an analytical result with non-zero delta"
    | (Analysis,Types.delta.Zero) -> analyticalScan input
    | (Hybrid,Types.delta.Range(_)) -> failwith "Cannot generate a hybrid result with non-zero delta"
    | (Hybrid,Types.delta.Zero) -> hybridScan input

