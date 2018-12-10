﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using LSPD_First_Response.Mod.API;
using ComputerPlus.Controllers.Models;
using ComputerPlus.Controllers;
using ComputerPlus.Interfaces.Reports.Models;

namespace ComputerPlus.Interfaces.ComputerPedDB
{

    class ComputerPedController
    {
        private readonly static List<ComputerPlusEntity> RecentSearches = new List<ComputerPlusEntity>();

        private static ComputerPlusEntity _LastSelected = null;
        public static ComputerPlusEntity LastSelected
        {
            get
            {
                if (_LastSelected != null && _LastSelected.Validate())
                {
                    return _LastSelected;
                }
                return null;
            }
            set
            {
                _LastSelected = value;
            }
        }

        internal List<Ped> PedsCurrentlyStoppedByPlayer
        {
            get
            {
                List<Ped> stoppedPeds = World.EnumeratePeds().Where(x => {
                    return (Functions.IsPedArrested(x) || Functions.IsPedGettingArrested(x) || Functions.IsPedStoppedByPlayer(x) 
                        || (x.Metadata.isStoppedByThisPlugin != null && x.Metadata.isStoppedByThisPlugin == true)); // added compatibility with Stop The Ped plugin
                }).ToList();

                LHandle pulloverHandle = Functions.GetCurrentPullover();
                if (pulloverHandle != null)
                {
                    Ped pulledOverSuspect = Functions.GetPulloverSuspect(pulloverHandle);
                    stoppedPeds.Add(pulledOverSuspect);
                    Vehicle pulledOverVehicle = pulledOverSuspect.CurrentVehicle;
                    if (pulledOverVehicle != null && pulledOverVehicle.IsValid() && pulledOverVehicle.Passengers.Count() > 0)
                    {
                        foreach (Ped passenger in pulledOverVehicle.Passengers)
                        {
                            stoppedPeds.Add(passenger);
                        }
                    }
                }

                return stoppedPeds;
            }
        }

        private ComputerPedController()
        {
            LastSelected = null;
        }

        internal ComputerPlusEntity LookupPersona(String name)
        {
            List<Ped> peds = World.GetAllPeds().ToList();
            peds.RemoveAll(p => !p || !p.Exists() || (p != null && !p.IsValid()));
            peds.OrderBy(p => p.DistanceTo(Game.LocalPlayer.Character.Position));
            var ped = peds.Where(p => p && Functions.GetPersonaForPed(p).FullName.ToLower().Equals(name.ToLower())).FirstOrDefault();
            return LookupPersona(ped);
        }
       
        internal ComputerPlusEntity LookupPersona(Ped ped)
        {
            if (ped == null || (ped != null && !ped.Exists())) return null;
            return insertEntityToRecentSearches(ComputerPlusEntity.CreateFrom(ped));
        }

        internal static ComputerPlusEntity insertEntityToRecentSearches(ComputerPlusEntity entity)
        {
            bool found = false;
            foreach (var ent in RecentSearches)
            {
                if (ent.FullName.Equals(entity.FullName) && ent.DOBString.Equals(entity.DOBString))
                {
                    found = true;
                    break;
                }
            }
            if (!found) RecentSearches.Add(entity);
            return entity;
        }

        internal List<ComputerPlusEntity> GetRecentSearches()
        {
            return RecentSearches;
        }

        internal static String GetRandomStreetAddress()
        {
            int streetNum = Globals.Random.Next(1, 5000);
            var street = World.GetStreetName(World.GetStreetHash(World.GetRandomPositionOnStreet()));
            return String.Format("{0} {1}", streetNum, street);
        }

        internal static String GetRandomWantedReason()
        {
            int index = Globals.Random.Next(0, Globals.WantedReasons.Count);
            return Globals.WantedReasons[index];
        }

        internal static CitationDefinition GetRandomCitation()
        {
            int index = Globals.Random.Next(0, Globals.CitationList.Count);
            return Globals.CitationList[index];
        }

        internal static void ShowPedSearch()
        {
            Globals.Navigation.Push(new ComputerPedSearch());
        }

        internal static void ShowPedView()
        {
            if (!LastSelected || !LastSelected.Validate()) return;
            try
            {
                var arrestReports = ComputerReportsController.GetArrestReportsForPed(LastSelected);
                if (arrestReports != null && arrestReports.Count > 0)
                    Function.Log("Found arrest report for ped");
                else
                    Function.Log("Arrest report for ped are null");

                var trafficCitations = ComputerReportsController.GetTrafficCitationsForPed(LastSelected);
                if (trafficCitations != null && trafficCitations.Count > 0)
                    Function.Log("Found citations for ped");
                else
                    Function.Log("Citations for ped are null");

                if (LastSelected != null && LastSelected.FullName != null && LastSelected.Ped != null && LastSelected.Ped.IsValid())
                {
                    Function.LogDebug("Creating ComputerPedViewExtendedContainer");

                    Globals.Navigation.Push(new ComputerPedViewExtendedContainer(new DetailedEntity(LastSelected, arrestReports, trafficCitations)));
                }
            } catch (Exception e)
            {
                Function.Log(e.ToString());
            }
        }

        public static ComputerPedController Instance
        {
            get;
            private set;
        } = new ComputerPedController();

    }
}
