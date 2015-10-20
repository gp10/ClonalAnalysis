﻿module SimulationCloneSizeDistribution

type cellPopulation = 
                {   A : int<Types.cell>;
                    B : int<Types.cell>;
                    C : int<Types.cell>;  }

type clone = {  population  :   cellPopulation;
                lambda  : float<Types.cell/Types.week>;
                rho     : float;
                r       : float;
                delta   : float;
                time    : float<Types.week>;
                rng     : System.Random;
                reportFrequency  : float<Types.week>;
                lastReportTime   : float<Types.week>;
                report  : cellPopulation option
                }
                with
                member this.gamma = this.lambda * this.rho / (1. - this.rho)
                member this.R = this.lambda * float(this.population.A) * 1.<Types.cell> + this.gamma * float(this.population.B) * 1.<Types.cell>
                member this.pAA =   this.r*(1.+this.delta)*this.lambda*float(this.population.A)*1.<Types.cell>/this.R
                member this.pAB =   (1.-2.*this.r)*this.lambda*float(this.population.A)*1.<Types.cell>/this.R
                member this.pBB =   this.r*(1.-this.delta)*this.lambda*float(this.population.A)*1.<Types.cell>/this.R
                member this.pB2C =  float(this.population.B)*1.<Types.cell>*this.gamma/this.R
                member this.update =    //Update time, report value
                                        let dt = - 1.<Types.week> * log (this.rng.NextDouble()/ (this.R*1.<Types.week Types.cell^-2> ) )
                                        let time'  = this.time + dt;
                                        let (lastReportTime',report') = if time' - this.lastReportTime > this.reportFrequency then (time',Some({A=this.population.A;B=this.population.B;C=this.population.C})) else (this.lastReportTime,None)
                                        //Selection an action
                                        let population'=    let random = this.rng.NextDouble()
                                                            //AA
                                                            if random < this.pAA then {this.population with A=this.population.A+1<Types.cell>}
                                                            //AB
                                                            else if random < this.pAA + this.pAB then {this.population with B=this.population.B+1<Types.cell>}
                                                            //BB
                                                            else if random < this.pAA + this.pAB + this.pBB then {this.population with A=this.population.A-1<Types.cell>;B=this.population.B+2<Types.cell>}
                                                            //Migration
                                                            else {this.population with B=this.population.B-1<Types.cell>;C=this.population.C+1<Types.cell>}
                                        {this with population = population'; time = time'; lastReportTime = lastReportTime' ; report = report' }

let initClone = {   population = {  A = 1<Types.cell>
                                    B = 0<Types.cell>
                                    C = 0<Types.cell>; }
                    lambda  = 2.<Types.cell/Types.week>
                    rho = 0.85
                    r = 0.15
                    delta = 0.
                    time = 0.<Types.week>
                    rng = System.Random()
                    reportFrequency = 4.<Types.week>
                    lastReportTime = 0.<Types.week>
                    report = None }

let rec simulate clone timeLimit trace = 
    match clone.time > timeLimit with
    | true -> List.rev trace
    | false ->  let clone' = clone.update
                match clone'.report with
                | None -> simulate clone' timeLimit trace
                | Some(state) -> simulate clone' timeLimit (state::trace)