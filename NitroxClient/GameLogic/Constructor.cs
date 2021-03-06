﻿using System;
using System.Collections.Generic;
using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Helper;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using NitroxModel.Packets;
using UnityEngine;
using static NitroxClient.GameLogic.Helper.TransientLocalObjectManager;

namespace NitroxClient.GameLogic
{
    public class MobileVehicleBay
    {
        private readonly IPacketSender packetSender;

        private readonly List<Type> interactiveChildTypes = new List<Type>() // we must sync guids of these types when creating vehicles (mainly cyclops)
        {
            { typeof(Openable) },
            { typeof(CyclopsLocker) },
            { typeof(Fabricator) },
            { typeof(FireExtinguisherHolder) }
        };

        public MobileVehicleBay(IPacketSender packetSender)
        {
            this.packetSender = packetSender;
        }

        public void BeginCrafting(GameObject constructor, TechType techType, float duration)
        {
            string constructorGuid = GuidHelper.GetGuid(constructor);

            Log.Debug("Building item from constructor with uuid: " + constructorGuid);

            Optional<object> opConstructedObject = TransientLocalObjectManager.Get(TransientObjectType.CONSTRUCTOR_INPUT_CRAFTED_GAMEOBJECT);

            if (opConstructedObject.IsPresent())
            {
                GameObject constructedObject = (GameObject)opConstructedObject.Get();

                List<InteractiveChildObjectIdentifier> childIdentifiers = ExtractGuidsOfInteractiveChildren(constructedObject);
                string constructedObjectGuid = GuidHelper.GetGuid(constructedObject);

                Optional<string> ModuleGuid = Optional<string>.Empty();
                if (techType == TechType.Cyclops)
                {
                    SubRoot subRoot = constructedObject.GetComponent<SubRoot>();
                    if (subRoot != null)
                    {
                        Log.Info("New Cyclop Modules Guid: " + GuidHelper.GetGuid(subRoot.upgradeConsole.modules.owner));
                        ModuleGuid = Optional<string>.Of(GuidHelper.GetGuid(subRoot.upgradeConsole.modules.owner));
                    }
                }

                ConstructorBeginCrafting beginCrafting = new ConstructorBeginCrafting(constructorGuid, constructedObjectGuid, ModuleGuid, techType, duration, childIdentifiers, constructedObject.transform.position, constructedObject.transform.rotation);
                packetSender.Send(beginCrafting);
            }
            else
            {
                Log.Error("Could not send packet because there wasn't a corresponding constructed object!");
            }
        }

        private List<InteractiveChildObjectIdentifier> ExtractGuidsOfInteractiveChildren(GameObject constructedObject)
        {
            List<InteractiveChildObjectIdentifier> ids = new List<InteractiveChildObjectIdentifier>();

            string constructedObjectsName = constructedObject.GetFullName() + "/";

            foreach (Type type in interactiveChildTypes)
            {
                Component[] components = constructedObject.GetComponentsInChildren(type, true);

                foreach (Component component in components)
                {
                    string guid = GuidHelper.GetGuid(component.gameObject);
                    string componentName = component.gameObject.GetFullName();
                    string relativePathName = componentName.Replace(constructedObjectsName, "");

                    ids.Add(new InteractiveChildObjectIdentifier(guid, relativePathName));
                }
            }

            return ids;
        }
    }
}
