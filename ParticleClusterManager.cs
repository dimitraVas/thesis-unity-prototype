using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Linq;

// Handles spatial clustering of particles based on proximity 
// Manages the creation, update and transformation of clusters.
// This component detects particle clusters in real time, computes centroids,
// Spawns or updates cluster objects, assigns particles to orbits,
// Maintains cluster state 

public class ParticleClusterManager : MonoBehaviour
{
    public SpawnManager spawnManager;           // Reference to the SpawnManager to access spawned particles
    public GameObject giantPlanetPrefab;        // The prefab for the giant planet
    private float clusterRadius = 150;           // The radius in which particles are considered part of the same cluster
    private int minParticlesInCluster = 3;       // Minimum number of particles required to form a cluster
    public float forceFieldRadius = 20f;        // The radius for the force field around the planet
    private float planetSizeMultiplier = 0.1f;   // The multiplier for adjusting the planet size based on the cluster size
    private float clusterCenterThreshold;   // The threshold for determining if a planet already exists near the cluster center
    public float planetUnityY = 200f;
    
    public List<List<int>> particleClusters;
    public List<GameObject> existingPlanets;
    public RegionManager regionManager;
    public Dictionary<GameObject, PlanetRegionData> planetRegionDictionary;
    
    private List<int> previousIndicesCount;
    public List<List<int>> previousParticleClusters;


    public event Action<List<int>> OnClusterUpdated; // Event triggered when clusters are formed/updated
    public int currentPlanetIndex;
    public float aloneParticlesYMovementAmount;
    public CoordinateConverter coordinateConverter;





    void Start()
    {
        aloneParticlesYMovementAmount = spawnManager.aloneParticlesYMovementAmount;
        spawnManager.OnParticleSpawned += CheckForClusters;
        clusterCenterThreshold = clusterRadius ;
        planetRegionDictionary = new Dictionary<GameObject, PlanetRegionData>();
        previousIndicesCount = new List<int>();
        previousParticleClusters = new List<List<int>>();
    }

    private void Update()
    {
        
    }

    private void CheckForClusters()
    {

        List<GameObject> particles = spawnManager.spawnedParticles;

        if (particles.Count < minParticlesInCluster) return;

        
        List<Vector2> particlePositions = new List<Vector2>();

        
        foreach (var particle in particles)
        {
            particlePositions.Add(new Vector2(particle.transform.position.x, particle.transform.position.z));
        }

        
        FilterParticlesWithinRadius(particlePositions, clusterRadius);

        

    }

    private void FilterParticlesWithinRadius(List<Vector2> particles, float radius)
    {
        

        // Stores all connections per particle in a dictionary
        Dictionary<int, List<int>> clusterPairs = new Dictionary<int, List<int>>();
        
        for (int i = 0; i < particles.Count; i++) 
        {
            List<int> clusterIndices = new List<int>();
            int count = 0;


            for (int j = 0; j < particles.Count; j++) 
            {
                if (i != j) // Don't compare the point to itself
                {
                    float distance = Vector2.Distance(particles[i], particles[j]);
                    if (distance <= radius)
                    {
                        count++;
                        clusterIndices.Add(j);
                    }
                    
                }
            }

            if (count>=minParticlesInCluster-1)
            {
                clusterPairs.Add(i, clusterIndices);
            }
        }
    
        // Step 2: Use the connection data to form fully connected clusters
        List<List<int>> clusters = GetClustersWithRadiusConstraint(clusterPairs,particles,clusterRadius);

        if (clusters != null)
        {
            particleClusters = clusters;

            ProcessClusters(clusters,particles);

            // Debug log particle count for each cluster
            for (int i = 0; i < particleClusters.Count; i++)
            {
                Debug.Log($"Cluster {i + 1} contains {particleClusters[i].Count} particles.");
            }

            
            AssignParticlesToOrbit(existingPlanets, particleClusters);
            

            for (int i = 0; i < previousParticleClusters.Count; i++)
            {
                string listContents = string.Join(", ", previousParticleClusters[i]);
                Debug.Log($"List {0}: [{listContents}]");
            }

            for (int i = 0; i < particleClusters.Count; i++)
            {
                string listContents = string.Join(", ", particleClusters[i]);
                Debug.Log($"List {1}: [{listContents}]");
            }

            
            if (!AreListsEqual(previousParticleClusters,particleClusters))
            {
                previousParticleClusters.Clear();
                Debug.Log("Clusters have been updated!");
                
                OnClusterUpdated?.Invoke(particleClusters[currentPlanetIndex]);

                foreach (var innerList in particleClusters)
                {
                    previousParticleClusters.Add(new List<int>(innerList));
                }
            }
        }

        // Debug the results to see the clusters formed
        Debug.Log($"Total Clusters Found: {clusters.Count}");

        for (int i = 0; i < clusters.Count; i++)
        {
            string clusterInfo = $"Cluster {i + 1}: ";
            clusterInfo += string.Join(", ", clusters[i]);
            Debug.Log(clusterInfo);
        }

        
    }
    private List<List<int>> GetClustersWithRadiusConstraint(Dictionary<int, List<int>> clusterPairs, List<Vector2> particles, float radius)
    {
        List<List<int>> clusters = new List<List<int>>();
        HashSet<int> visited = new HashSet<int>();

        foreach (var pair in clusterPairs)
        {
            int particleIndex = pair.Key;
            if (visited.Contains(particleIndex)) continue;

            List<int> cluster = new List<int>();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(particleIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (visited.Contains(current)) continue;

                visited.Add(current);

                if (IsParticleValidForCluster(cluster, particles[current], particles, radius))
                {
                    cluster.Add(current);

                    if (clusterPairs.ContainsKey(current))
                    {
                        foreach (int neighbor in clusterPairs[current])
                        {
                            if (!visited.Contains(neighbor) && !queue.Contains(neighbor))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }

            if (cluster.Count >= minParticlesInCluster)
            {
                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    private bool IsParticleValidForCluster(List<int> cluster, Vector2 newParticlePosition, List<Vector2> particles, float radius)
    {
        if (cluster.Count == 0) return true;

        // Calculate the new centroid
        Vector2 sum = Vector2.zero;
        foreach (int index in cluster)
        {
            sum += particles[index];
        }
        sum += newParticlePosition; // Add the new particle to the sum
        Vector2 newCentroid = sum / (cluster.Count + 1);

        // Check if all old particles are still within the radius
        foreach (int index in cluster)
        {
            float distance = Vector2.Distance(newCentroid, particles[index]);
            if (distance > radius)
            {
                return false; // Old particle is outside the valid radius
            }
        }

        return true;
    }

    

    
    // Process clusters and spawn a planet for each
    private void ProcessClusters(List<List<int>> clusters, List<Vector2> particles)
    {
        //Number of Clusters
        for (int i = 0; i < clusters.Count; i++)
        {
            List<int> indexOfEachCluster = clusters[i];
            CalculateClusterCentroid(indexOfEachCluster, particles);

        }
    }

    // Method to calculate the centroid of a cluster
    private void CalculateClusterCentroid(List<int> cluster, List<Vector2> particlePositions)
    {
        Vector2 sum = Vector2.zero;

        foreach (int index in cluster)
        {
            sum += particlePositions[index];
        }

        Vector2 centroid2D = sum / cluster.Count;
        // Convert the 2D centroid to a 3D position 
        Vector3 centroid3D = new Vector3(centroid2D.x, planetUnityY, centroid2D.y);
        SpawnGiantPlanet(centroid3D);
        



    }


    // Method to spawn a giant planet at a specified position
    private void SpawnGiantPlanet(Vector3 position)
    {
        

        // Declare a variable to hold the index of the existing planet
        int planetIndex;
        
        if (IsExistingPlanet(position, out planetIndex))
        {
            
            //Debug.Log(" There is a planet");   
            // Move the existing planet to the new centroid position
            Transform parentTransform = existingPlanets[planetIndex].transform.parent;

            if (parentTransform != null)
            {
                parentTransform.position = position;
                Debug.Log($"Moved parent of planet at index {planetIndex} to position: {position}");
            }
            else
            {
                Debug.LogWarning($"Planet at index {planetIndex} has no parent. Moving the planet itself.");
                existingPlanets[planetIndex].transform.position = position;
            }


            UpdatePlanetSize(existingPlanets[planetIndex], particleClusters[planetIndex].Count, minParticlesInCluster, planetIndex, previousIndicesCount);
            UpdatePlanetToDictionary(existingPlanets[planetIndex], planetIndex);
            

            
        }

        else
        {

            //Debug.Log(" No planet");   
            GameObject giantPlanet = Instantiate(giantPlanetPrefab, position, Quaternion.identity);
            existingPlanets.Add(giantPlanet);
            if (giantPlanet != null)
            {
                
                AddPlanetToDictionary(existingPlanets[existingPlanets.Count-1], existingPlanets.Count - 1);
                previousIndicesCount.Add(minParticlesInCluster);

                

                
            }
            else
            {
                Debug.LogError("Planet object is null.");
            }
            
            
            
        }
        
    }

    // Method to find an existing planet near the centroid
    private bool IsExistingPlanet(Vector3 centroid, out int planetIndex)
    {
        planetIndex = -1; // Default to -1 to indicate not found
        int index = 0;    // Auxiliary variable to track the index

        foreach (GameObject planet in existingPlanets)
        {
            float distance = Vector2.Distance(new Vector2(planet.transform.position.x, planet.transform.position.z), new Vector2(centroid.x, centroid.z));

            if (distance <= clusterCenterThreshold)
            {
                planetIndex = index; // Found the planet, return the index
                return true; // Found a planet within the threshold
            }
            index++;
        }
        return false; // No planet found within the threshold
    }

    private void AddPlanetToDictionary(GameObject planet, int planetIndex)
    {
        // Get the 3D position of the planet and convert it to a 2D coordinate
        Vector3 planetPosition = planet.transform.position;
        Vector2 planetCoordinate= new Vector2 (planetPosition.x, planetPosition.z);
        (CityRegion region, SubRegion subRegion) = regionManager.GetRegionAndSubRegion(planetCoordinate);

        PlanetRegionData data = new PlanetRegionData(planet, planetIndex, region, subRegion);
        planetRegionDictionary.Add(planet, data);
        
    }

    private void UpdatePlanetToDictionary(GameObject planet, int planetIndex)
    {
        Vector3 planetPosition=planet.transform.position;
        Vector2 planetCoordinate = new Vector2(planetPosition.x, planetPosition.z);
        (CityRegion region, SubRegion subRegion) = regionManager.GetRegionAndSubRegion(planetCoordinate);

        // Update the dictionary with the new data
        planetRegionDictionary[planet] = new PlanetRegionData(
            planet,           // The planet GameObject
            planetIndex,      // The index of the planet
            region,           // The region the planet is in
            subRegion
            
            );// The subregion the planet is in (if any)  
    }


    private void UpdatePlanetSize(GameObject planet, int clusterSize, int minClusterSize, int planetIndex, List<int> previousIndicesCounts)
    {
        if (previousIndicesCounts[planetIndex] == clusterSize)
        {

            return;
        }
        else
        {
            //  get the default scale
            Vector3 originalScale = planet.transform.localScale;

            // Calculate the new size based on the cluster size

            float scaleModifier = 1 + ((clusterSize - minClusterSize) * planetSizeMultiplier);
            Vector3 newScale = scaleModifier * originalScale;
            planet.transform.localScale = newScale;
            previousIndicesCount[planetIndex] = clusterSize;
        }

    }

    public void AssignParticlesToOrbit(List<GameObject> planet, List<List<int>> particleIndices)
    {
        for (int i = 0; i < planet.Count; i++) // Loop through each planet
        {
            foreach (int particleIndex in particleIndices[i]) // Access each particle index for the current planet
            {
                // Get the particle GameObject using the particle index
                GameObject particle = spawnManager.GetParticleByIndex(particleIndex);

                if (particle != null) // If the particle exists
                {
                    // Add the ParticleOrbit script to the particle
                    ParticleOrbit orbitScript = particle.AddComponent<ParticleOrbit>();

                    // Set up the orbital parameters
                    orbitScript.planet = planet[i].transform; // Assign the planets transform to the orbit script
                    orbitScript.particle=particle.transform;
                    orbitScript.orbitRadius = Random.Range(10f, 50f);  // Random orbit radius
                    orbitScript.orbitSpeed = Random.Range(0.1f, 1f);   // Random orbit speed
                }
                else
                {
                    Debug.LogWarning($"Particle at index {particleIndex} not found.");
                }
            }
        }

        CreateParent(particleIndices);

    }



    private void CreateParent(List<List<int>> particleIndices)
    {
        for (int i = 0; i < particleIndices.Count; i++)
        {
            // Name for the cluster parent
            string clusterName = $"Cluster_{i}";

            // Check if a GameObject with the same name already exists
            GameObject clusterParent = GameObject.Find(clusterName);

            if (clusterParent == null) // If it doesn't exist, create it
            {
                clusterParent = new GameObject(clusterName);
            }
            else
            {
                Debug.Log($"GameObject with the name {clusterName} already exists.");
            }

            foreach (int particleIndex in particleIndices[i]) // Access each particle index for the current cluster
            {
                GameObject particle = spawnManager.GetParticleByIndex(particleIndex); // Get the particle GameObject using the particle index

                if (particle != null) // Ensure the particle exists
                {
                    // Set the parent of the particle to the clusterParent GameObject
                    particle.transform.SetParent(clusterParent.transform);
                }
                else
                {
                    Debug.LogWarning($"Particle with index {particleIndex} could not be found!");
                }
            }
        }
        // NEEEEEEEEEW
        particleClusters = particleIndices;
        AssignClustersToPlanet();
    }

    private void AssignClustersToPlanet()
    {
        

        // Iterate through each cluster parent by name and assign it to the planet as its child
        for (int i = 0; i < particleClusters.Count; i++)
        {
            string clusterName = $"Cluster_{i}";

            // Find the cluster GameObject
            GameObject clusterParent = GameObject.Find(clusterName);

            if (clusterParent != null)
            {
                // Set the planet as the parent of the cluster
                clusterParent.transform.SetParent(existingPlanets[i].transform);
            }
            else
            {
                Debug.LogWarning($"Cluster parent '{clusterName}' could not be found!");
            }
        }
    }

    public void AssignParticlesToYValues(List<float> updatedYValues, List<int> particleIndices)
    {
        // Ensure the lists have the same count
        if (updatedYValues.Count != particleIndices.Count)
        {
            Debug.LogError($"The counts of updatedYValues ({updatedYValues.Count}) and particleIndices ({particleIndices.Count}) do not match!");
            return;
        }

        // Iterate through the particle indices and assign the Y values
        for (int i = 0; i < particleIndices.Count; i++)
        {
            // Get the actual particle index from the particleIndices list
            int particleIndex = particleIndices[i];

            // Get the particle GameObject using the particle index
            GameObject particle = spawnManager.GetParticleByIndex(particleIndex);

            if (particle != null)
            {
                // Create a new position with the updated Y value and retain the current X and Z values
                Vector3 currentPosition = particle.transform.position;
                currentPosition.y = updatedYValues[i];

                // Apply the updated position to the particle
                particle.transform.position = currentPosition;
            }
            else
            {
                Debug.LogWarning($"Particle at index {particleIndex} is null.");
            }
        }
    }

    private bool AreListsEqual(List<List<int>> previous, List<List<int>> newer)
    {
        // Check if the outer lists are of the same size
        if (previous.Count != newer.Count) 
        {
            Debug.Log("Outer lists have different sizes");
            currentPlanetIndex = newer.Count - 1;
            return false;
        }


        for (int i = 0; i < previous.Count; i++)
        {
            if (previous[i].Count != newer[i].Count)
            {
                Debug.Log($"Inner lists at index {i} have different sizes");
                currentPlanetIndex = i;
                return false;
            }

            for (int j = 0; j < previous[i].Count; j++)
            {
                if (previous[i][j] != newer[i][j])
                {
                    currentPlanetIndex = i;
                    Debug.Log($"Mismatch found at index {i}, {j}: {previous[i][j]} != {newer[i][j]}");
                    return false;
                }
            }
        }

        Debug.Log("All lists are equal");
        return true;
    }

    public void MovePlanetsBasedOnEmotion(List<int> planetsToMove, float directionMultiplier)
    {
        if (planetsToMove.Count < 1) return;

        for (int i = 0; i < planetsToMove.Count; i++)
        {
            int index = planetsToMove[i]; // Retrieve the index to move
            if (index >= 0 && index < previousIndicesCount.Count) // Ensure the index is valid
            {
                GameObject planet = existingPlanets[index];

                // Modify the y-coordinate of the particle's position
                Vector3 newPosition = planet.transform.position;
                newPosition.y = coordinateConverter.unityY + (directionMultiplier * aloneParticlesYMovementAmount);
                planet.transform.position = newPosition;
            }
            else
            {
                Debug.LogWarning($"Invalid index {index} for spawnedParticles list.");
            }
        }
    }

}




