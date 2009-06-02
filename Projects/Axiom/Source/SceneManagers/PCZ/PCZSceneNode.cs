﻿using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCZSceneNode : SceneNode
    {
        Vector3 newPosition;
        PCZone homeZone;
        bool anchored;
        bool allowedToVisit;
        readonly Dictionary<string, PCZone> visitingZones = new Dictionary<string, PCZone>();
        Vector3 prevPosition;
        ulong lastVisibleFrame;
        PCZCamera lastVisibleFromCamera;
        Dictionary<string, ZoneData> zoneData = new Dictionary<string, ZoneData>();
        bool enabled;
        Dictionary<string, MovableObject> objectsByName = new Dictionary<string, MovableObject>();


        public PCZSceneNode(SceneManager creator)
            : base(creator)
        {
            homeZone = null;
            anchored = false;
            allowedToVisit = true;
            lastVisibleFrame = 0;
            lastVisibleFromCamera = null;
            enabled = true;
        }

        public PCZSceneNode(SceneManager creator, string name)
            : base(creator, name)
        {
            homeZone = null;
            anchored = false;
            allowedToVisit = true;
            lastVisibleFrame = 0;
            lastVisibleFromCamera = null;
            enabled = true;
        }

        ~PCZSceneNode()
        {
            // clear visiting zones list
            visitingZones.Clear();

            // delete zone data
            zoneData.Clear();

            //clear object list
            objectsByName.Clear();
        }

        #region Propertys

        public Vector3 PreviousPosition
        {
            get { return prevPosition; }
        }

        public bool IsAnchored
        {
            get
            {
                return anchored;
            }
            set
            {
                anchored = value;
            }
        }

        public bool AllowToVisit
        {
            get
            {
                return allowedToVisit;
            }
            set
            {
                allowedToVisit = value;
            }
        }

        public ulong LastVisibleFrame
        {

            get
            {
                return lastVisibleFrame;
            }
            set
            {
                lastVisibleFrame = value;
            }
        }

        public PCZCamera LastVisibleFromCamera
        {
            get
            {
                return lastVisibleFromCamera;
            }
            set
            {
                lastVisibleFromCamera = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                enabled = value;
            }
        }


        #endregion Propertys

        #region Methods

    protected override void Update(bool updateChildren, bool parentHasChanged)
	{
	    base.Update(updateChildren, parentHasChanged);

		prevPosition = newPosition;
		newPosition = DerivedPosition;   // do this way since _update is called through SceneManager::_updateSceneGraph which comes before PCZSceneManager::_updatePCZSceneNodes
	}
    //-----------------------------------------------------------------------
	public override SceneNode CreateChildSceneNode(Vector3 translate, Quaternion rotate)
	{
		PCZSceneNode childSceneNode = (PCZSceneNode)(this.CreateChild(translate, rotate));
		if (anchored)
		{
			childSceneNode.AnchorToHomeZone(homeZone);
			homeZone.AddNode(childSceneNode);
		}
		return childSceneNode;
	}
    //-----------------------------------------------------------------------
    public override SceneNode CreateChildSceneNode(string name, Vector3 translate, Quaternion rotate)
	{
		PCZSceneNode childSceneNode = (PCZSceneNode)(this.CreateChild(name, translate, rotate));
		if (anchored)
		{
			childSceneNode.AnchorToHomeZone(homeZone);
			homeZone.AddNode(childSceneNode);
		}
		return childSceneNode;
	}


        public PCZone HomeZone
        {
            get
            {
                return homeZone;
            }
            set
            {
                // if the new home zone is different than the current, remove 
                // the node from the current home zone's list of home nodes first
                if (value != homeZone && homeZone != null)
                {
                    homeZone.RemoveNode(this);
                }

                homeZone = value;
            }
        }

	public void AnchorToHomeZone(PCZone zone)
	{
		homeZone = zone;
		anchored = true;
	}

	public void AddZoneToVisitingZonesMap(PCZone zone)
	{
		visitingZones[zone.Name] = zone;
	}
	public void ClearVisitingZonesMap()
	{
		visitingZones.Clear();
	}
	/* The following function does the following:
	 * 1) Remove references to the node from zones the node is visiting
	 * 2) Clear the node's list of zones it is visiting
	 */
	public void ClearNodeFromVisitedZones()
	{
		if (visitingZones.Count > 0)
		{
			// first go through the list of zones this node is visiting 
			// and remove references to this node
			//PCZone zone;
			//ZoneMap::iterator it = mVisitingZones.begin();

		    foreach (PCZone zone in visitingZones.Values)
		    {
		        zone.RemoveNode(this);
		    }

			// second, clear the visiting zones list
			visitingZones.Clear();
		}
	}

	/* Remove all references that the node has to the given zone
	*/
	public void RemoveReferencesToZone(PCZone zone)
	{
		if (homeZone == zone)
		{
			homeZone = null;
		}

        if(visitingZones.ContainsKey(zone.Name))
        {
            visitingZones.Remove(zone.Name);
        }

		// search the map of visiting zones and remove
        //ZoneMap::iterator i;
        //i = mVisitingZones.find(zone->getName());
        //if (i != mVisitingZones.end())
        //{
        //    mVisitingZones.erase(i);
        //}
	}
	
	/* returns true if zone is in the node's visiting zones map
	   false otherwise.
	*/
	public bool IsVisitingZone(PCZone zone)
	{
        if ( visitingZones.ContainsKey( zone.Name ) )
        {
            return true;
        }

	    return false;
		
        //ZoneMap::iterator i;
        //i = mVisitingZones.find(zone->getName());
        //if (i != mVisitingZones.end())
        //{
        //    return true;
        //}
        //return false;
	}

    /** Adds the attached objects of this PCZSceneNode into the queue. */
    public void AddToRenderQueue( Camera cam, RenderQueue queue, bool onlyShadowCasters, VisibleObjectsBoundsInfo visibleBounds )
    {
        foreach (KeyValuePair<string, MovableObject> pair in objectsByName)
        {
            pair.Value.NotifyCurrentCamera(cam);

            if(pair.Value.IsVisible && (!onlyShadowCasters || pair.Value.CastShadows))
            {
                pair.Value.UpdateRenderQueue(queue);

                if(!visibleBounds.aabb.IsNull)
                {
                    visibleBounds.Merge(pair.Value.GetWorldBoundingBox(true), pair.Value.GetWorldBoundingSphere(true), cam);
                }
            }
        }
    }

	/** Save the node's current position as the previous position
	*/
	public void SavePrevPosition()
	{
		prevPosition = DerivedPosition;
	}

	public void SetZoneData(PCZone zone, ZoneData zoneData)
	{
        
		// first make sure that the data doesn't already exist
		if (this.zoneData.ContainsKey(zone.Name))
		{
            throw new AxiomException("A ZoneData associated with zone " + zone.Name + " already exists. PCZSceneNode::setZoneData" );
		}

		//mZoneData[zone->getName()] = zoneData;
        // is this equivalent? i think so...
        this.zoneData.Add(zone.Name, zoneData);
	}

	// get zone data for this node for given zone
	// NOTE: This routine assumes that the zone data is present!
	public ZoneData GetZoneData(PCZone zone)
	{
		return zoneData[zone.Name];
	}

	// update zone-specific data for any zone that the node is touching
	public void UpdateZoneData()
	{
	    ZoneData zoneData;
	    PCZone zone;

	    // make sure home zone data is updated
	    zone = homeZone;
	    if (zone.RequiresZoneSpecificNodeData)
	    {
	        zoneData = GetZoneData(zone);
	        zoneData.update();
	    }

	    // update zone data for any zones visited
	    foreach (KeyValuePair<string, PCZone> pair in visitingZones)
	    {
	        zone = pair.Value;

	        if (zone.RequiresZoneSpecificNodeData)
	        {
	            zoneData = GetZoneData(zone);
	            zoneData.update();
	        }
	    }
	}

        #endregion Methods
    }
}