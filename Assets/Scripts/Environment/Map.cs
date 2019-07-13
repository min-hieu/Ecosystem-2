﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The map is divided into n x n regions, with a list of entities for each region
// This allows an entity to more quickly find other nearby entities
public class Map {
    public readonly List<LivingEntity>[, ] map;
    readonly Vector2[, ] centres;
    readonly int regionSize;
    readonly int numRegions;

    public Map (int size, int regionSize) {
        this.regionSize = regionSize;
        numRegions = Mathf.CeilToInt (size / (float) regionSize);
        map = new List<LivingEntity>[numRegions, numRegions];
        centres = new Vector2[numRegions, numRegions];

        for (int y = 0; y < numRegions; y++) {
            for (int x = 0; x < numRegions; x++) {
                Coord regionBottomLeft = new Coord (x * regionSize, y * regionSize);
                Coord regionTopRight = new Coord (x * regionSize + regionSize, y * regionSize + regionSize);
                Vector2 centre = (Vector2) (regionBottomLeft + regionTopRight) / 2f;
                centres[x, y] = centre;
                map[x, y] = new List<LivingEntity> ();
            }
        }
    }

    // Calculates coordinates of all regions that may contain entities within view from the specified viewDoord/viewDstance
    public List<Coord> GetRegionsInView (Coord viewCoord, float viewDistance) {
        List<Coord> regions = new List<Coord> ();
        int originRegionX = viewCoord.x / regionSize;
        int originRegionY = viewCoord.y / regionSize;
        float sqrViewDst = viewDistance * viewDistance;
        Vector2 viewCentre = viewCoord + Vector2.one * .5f;

        int searchNum = Mathf.Max (1, Mathf.CeilToInt (viewDistance / regionSize));
        // Loop over all regions that might be within the view dst to check if they actually are
        for (int offsetY = -searchNum; offsetY <= searchNum; offsetY++) {
            for (int offsetX = -searchNum; offsetX <= searchNum; offsetX++) {
                int viewedRegionX = originRegionX + offsetX;
                int viewedRegionY = originRegionY + offsetY;

                if (viewedRegionX >= 0 && viewedRegionX < numRegions && viewedRegionY >= 0 && viewedRegionY < numRegions) {
                    // Calculate distance from view coord to closest edge of region to test if region is in range
                    float ox = Mathf.Max (0, Mathf.Abs (viewCentre.x - centres[viewedRegionX, viewedRegionY].x) - regionSize / 2f);
                    float oy = Mathf.Max (0, Mathf.Abs (viewCentre.y - centres[viewedRegionX, viewedRegionY].y) - regionSize / 2f);
                    float sqrDstFromRegionEdge = ox * ox + oy * oy;
                    if (sqrDstFromRegionEdge <= sqrViewDst) {
                        regions.Add (new Coord (viewedRegionX, viewedRegionY));
                    }
                }
            }
        }
        return regions;
    }

    public void Add (LivingEntity e, Coord coord) {
        int regionX = coord.x / regionSize;
        int regionY = coord.y / regionSize;

        int index = map[regionX, regionY].Count;
        // store the entity's index in the list inside the entity itself for quick access
        e.mapIndex = index;
        e.mapCoord = coord;
        map[regionX, regionY].Add (e);
    }

    public void Remove (LivingEntity e, Coord coord) {
        int regionX = coord.x / regionSize;
        int regionY = coord.y / regionSize;

        int index = e.mapIndex;
        int lastElementIndex = map[regionX, regionY].Count - 1;
        // If this entity is not last in the list, put the last entity in its place
        if (index != lastElementIndex) {
            map[regionX, regionY][index] = map[regionX, regionY][lastElementIndex];
            map[regionX, regionY][index].mapIndex = e.mapIndex;
        }
        // Remove last entity from the list
        map[regionX, regionY].RemoveAt (lastElementIndex);
    }

    public void Move (LivingEntity e, Coord fromCoord, Coord toCoord) {
        Remove (e, fromCoord);
        Add (e, toCoord);
    }

    public void DrawDebugGizmos (Coord coord, float viewDst) {
        // Settings:
        bool showViewedRegions = false;
        bool showOccupancy = true;
        float height = Environment.tileCentres[0, 0].y + 0.1f;
        Gizmos.color = Color.black;

        // Draw:
        int regionX = coord.x / regionSize;
        int regionY = coord.y / regionSize;

        // Draw region lines
        for (int i = 0; i <= numRegions; i++) {
            Gizmos.DrawLine (new Vector3 (i * regionSize, height, 0), new Vector3 (i * regionSize, height, regionSize * numRegions));
            Gizmos.DrawLine (new Vector3 (0, height, i * regionSize), new Vector3 (regionSize * numRegions, height, i * regionSize));
        }

        // Draw region centres
        for (int y = 0; y < numRegions; y++) {
            for (int x = 0; x < numRegions; x++) {
                Vector3 centre = new Vector3 (centres[x, y].x, height, centres[x, y].y);
                Gizmos.DrawSphere (centre, .3f);
            }
        }
        // Highlight regions in view
        if (showViewedRegions) {
            List<Coord> regionsInView = GetRegionsInView (coord, viewDst);

            for (int y = 0; y < numRegions; y++) {
                for (int x = 0; x < numRegions; x++) {
                    Vector3 centre = new Vector3 (centres[x, y].x, height, centres[x, y].y);
                    foreach (var regionInView in regionsInView) {
                        if (regionInView.x == x && regionInView.y == y) {
                            bool isCurrentRegion = x == regionX && y == regionY;
                            var prevCol = Gizmos.color;
                            Gizmos.color = (isCurrentRegion) ? new Color (1, 0, 0, .5f) : new Color (1, 0, 0, .25f);
                            Gizmos.DrawCube (centre, new Vector3 (regionSize, .1f, regionSize));
                            Gizmos.color = prevCol;
                        }
                    }
                }
            }
        }

        if (showOccupancy) {
            int maxOccupants = 0;
            for (int y = 0; y < numRegions; y++) {
                for (int x = 0; x < numRegions; x++) {
                    maxOccupants = Mathf.Max (maxOccupants, map[x, y].Count);
                }
            }
            if (maxOccupants > 0) {
                for (int y = 0; y < numRegions; y++) {
                    for (int x = 0; x < numRegions; x++) {
                        Vector3 centre = new Vector3 (centres[x, y].x, height, centres[x, y].y);
                        int numOccupants = map[x, y].Count;
                        if (numOccupants > 0) {
                            var prevCol = Gizmos.color;
                            Gizmos.color = new Color (1, 0, 0, numOccupants / (float) maxOccupants);
                            Gizmos.DrawCube (centre, new Vector3 (regionSize, .1f, regionSize));
                            Gizmos.color = prevCol;
                        }
                    }
                }
            }
        }
    }
}