using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.Collections;


public class ClusterEmotions : MonoBehaviour
{
    
    public ParticleClusterManager particleClusterManager;
    private List<int> updatedClusters;
    private bool isClusterUpdated = false;
    private bool isEmotionAssigned = false;

    public List<List<string>> totalEmotionsForClusters;

    public List<float> updatedYValues;
    public List<string> maxEmotions;

    public SpawnManager spawnManager;
    public List<List<int>> particleClusters;

    [SerializeField]
    public List<List<string>> clusterProminentColors;
    

    void Start()
    {
        if (particleClusterManager != null)
        {
            List<ParticleData> particles = ParticleEmotion.particleDataList;
            // Listen to both events
            particleClusterManager.OnClusterUpdated += UpdateClusterEmotions;
            ParticleEmotion.onParticleDataUpdated += UpdateEmotionAssigned;
            ParticleEmotion.onParticleDataUpdated += CheckAllParticles;
        }
        else
        {
            Debug.LogError("ParticleClusterManager is not assigned!");
        }

        if (clusterProminentColors == null)
        {
            clusterProminentColors = new List<List<string>>();
        }

        totalEmotionsForClusters = new List<List<string>>();
    }

    // Update is called once per frame
    void Update()
    {
        particleClusters = particleClusterManager.previousParticleClusters;
    }


    // Event handler for cluster update
    private void UpdateClusterEmotions(List<int> particleCluster)
    {
        isClusterUpdated = true;
        updatedClusters= particleCluster;
        
        // Now check if emotion assignment has already happened
        CheckEventsAndProcess();
    }

    // Event handler for emotion assignment
    private void UpdateEmotionAssigned()
    {
        isEmotionAssigned = true;

        // Now check if cluster update has already happened
        CheckEventsAndProcess();
    }

    // Check if both events have happened and process the data
    private void CheckEventsAndProcess()
    {
        if (isClusterUpdated && isEmotionAssigned)
        {

            // Both events have happened, so process the emotions for the updated cluster
            
            
            CountEmotions(updatedClusters);
            isClusterUpdated= false;
            isEmotionAssigned= false;   
        }
        else
        {
            isEmotionAssigned=false;
        }
    }


    private void CountEmotions(List<int> updatedCluster)
    {
        // Dictionary to count emotions in the updated cluster
        Dictionary<string, int> emotionCounts = new Dictionary<string, int>();

        // Get the count of particles in the particle data list
        int totalParticles = ParticleEmotion.particleDataList.Count;
        
        // Go through each particle index in the updated cluster
        foreach (var particleIndex in updatedCluster)
        {
            // Ensure the index is valid
            if (particleIndex >= 0 && particleIndex < totalParticles)
            {
                var particleData = ParticleEmotion.particleDataList[particleIndex];

                if (particleData != null)
                {
                    string emotionCategory = particleData.emotionCategory; 

                    // Count emotions
                    if (!emotionCounts.ContainsKey(emotionCategory))
                    {
                        emotionCounts[emotionCategory] = 0;
                    }
                    emotionCounts[emotionCategory]++;
                }
                else
                {
                    Debug.LogWarning($"ParticleData at index {particleIndex} is null.");
                }
            }
            else
            {
                // Log the invalid index and also the count of the available particle data
                Debug.LogWarning($"Invalid particle index: {particleIndex}. Available particles: {totalParticles}");
            }
        }

        // Now find the most prominent emotion
        FindMostProminentEmotion(emotionCounts);
    }

    private void FindMostProminentEmotion(Dictionary<string, int> emotionCounts)
    {
        List<string> mostProminentEmotions = new List<string>();
        int maxCount = 0;

        // Iterate through the dictionary to find the emotion(s) with the highest count
        foreach (var entry in emotionCounts)
        {
            if (entry.Value > maxCount)
            {
                // New maximum found, reset the list
                mostProminentEmotions.Clear();
                mostProminentEmotions.Add(entry.Key);
                maxCount = entry.Value;
            }
            else if (entry.Value == maxCount)
            {
                // If the count is equal to the max count, add the emotion to the list
                mostProminentEmotions.Add(entry.Key);
            }
        }

        if (mostProminentEmotions.Count > 0)
        {
            string emotionsList = string.Join(", ", mostProminentEmotions);
            Debug.Log($"The most prominent emotion(s): {emotionsList} with a count of {maxCount}.");
        }
        else
        {
            Debug.Log("No emotions found in the cluster.");
        }

        SortParticlesBasedOnEmotion(emotionCounts);
        UpdateClusterEmotionsList(mostProminentEmotions);

    }

    private void SortParticlesBasedOnEmotion(Dictionary<string, int> emotionCounts)
    {
        bool isMinValuesEqual = false;
        bool isMaxValuesEqual = false;
        bool isAllValuesEqual = false;
        // Step 1: Extract keys and values from the dictionary into separate lists
        List<string> emotions = new List<string>(emotionCounts.Keys);
        List<int> counts = new List<int>(emotionCounts.Values);

        // Step 2: Pair the keys and values together
        var pairedList = counts.Select((value, index) => new { Value = value, Key = emotions[index] }).ToList();

        // Step 3: Sort the paired list based on the values (counts)
        pairedList = pairedList.OrderBy(pair => pair.Value).ToList();

        // Step 4: Extract the sorted lists
        List<int> sortedCounts = pairedList.Select(pair => pair.Value).ToList();
        List<string> sortedEmotions = pairedList.Select(pair => pair.Key).ToList();

        // Debug the sorted results
        Debug.Log("Sorted Counts: " + string.Join(", ", sortedCounts));   // Prints: e.g., 2, 2, 5
        Debug.Log("Sorted Emotions: " + string.Join(", ", sortedEmotions)); // Prints: e.g., Negative, Neutral, Positive

        // Step 5: Analyze the sorted counts
        var groupedCounts = sortedCounts.GroupBy(value => value).ToList();

        if (groupedCounts.Count == 1)
        {
            // All values are the same
            isAllValuesEqual = true;
            
        }
        else
        {
            // Compare the first and last groups
            int minValueGroupCount = groupedCounts[0].Count();
            int maxValueGroupCount = groupedCounts[groupedCounts.Count - 1].Count();

            if (minValueGroupCount > 1)
            {
                isMinValuesEqual = true;
            }

            if (maxValueGroupCount > 1)
            {
                isMaxValuesEqual = true;
            }
        }

        // Debug the results
        //Debug.Log($"All Values Equal: {isAllValuesEqual}");
        //Debug.Log($"Min Values Equal: {isMinValuesEqual}");
        //Debug.Log($"Max Values Equal: {isMaxValuesEqual}");


        // Proceed to arrange particles
        List<ParticleData> clusterParticles = new List<ParticleData>();
        List<int> particlesIndex = particleClusterManager.particleClusters[particleClusterManager.currentPlanetIndex];
        //Debug.Log("Particles Index List: " + string.Join(", ", particlesIndex));

        
        

        foreach (int index in particlesIndex)
        {
            
            foreach (ParticleData particle in ParticleEmotion.particleDataList)
            {
                if (index == particle.index)
                {
                    
                    clusterParticles.Add(particle);
                    
                }
            }
        }

        

        updatedYValues =ArrangeParticlesToParent(sortedCounts, sortedEmotions, isAllValuesEqual, isMinValuesEqual, isMaxValuesEqual, clusterParticles);
        particleClusterManager.AssignParticlesToYValues(updatedYValues, particleClusterManager.particleClusters[particleClusterManager.currentPlanetIndex]);
        updatedClusters.Clear();
        CreateClustersListOfEmotions(particleClusters.Count-1, sortedEmotions, isAllValuesEqual, isMinValuesEqual, isMaxValuesEqual);
        clusterParticles.Clear();
        particlesIndex.Clear();
    }

    public List<float> ArrangeParticlesToParent(List<int> sortedCounts, List<string> sortedEmotions, bool isAllValuesEqual, bool isMinValuesEqual, bool isMaxValuesEqual, List<ParticleData> particles)
    {
        int currentPlanetIndex = particleClusterManager.currentPlanetIndex;

        //Get the scale of the Cluster's planet
        Transform currentPlanet = particleClusterManager.existingPlanets[currentPlanetIndex].transform;

        // Create a list to store Y values
        List<float> yResults = new List<float>();

        if (isAllValuesEqual)
        {
            foreach (ParticleData particle in particles)
            {
                // Modify the Y position of the planet
                float yResult = currentPlanet.position.y - currentPlanet.localScale.y / 2; // Get the current position
                yResults.Add(yResult);
                
                
                
            }
            

        }

        else if (isMinValuesEqual)
        {
            foreach (ParticleData particle in particles)
            {
                if (particle.emotionCategory == sortedEmotions[0])
                {
                    float yResult = currentPlanet.position.y;
                    yResults.Add(yResult);
                }
                else
                {
                    float yResult = currentPlanet.position.y - currentPlanet.localScale.y / 2;
                    yResults.Add(yResult);
                }
            }
            
        }

        else if (isMaxValuesEqual)
        {
            foreach (ParticleData particle in particles)
            {
                if (particle.emotionCategory == sortedEmotions[2])
                {
                    float yResult = currentPlanet.position.y - currentPlanet.localScale.y / 2;
                    yResults.Add(yResult);
                }
                else
                {
                    float yResult = currentPlanet.position.y;
                    yResults.Add(yResult);
                }
            }
            
        }
        else
        {

            foreach (ParticleData particle in particles)
            {
                if (sortedCounts.Count == 3)
                {
                    if (particle.emotionCategory == sortedEmotions[0])
                    {
                        float yResult = currentPlanet.position.y + currentPlanet.localScale.y / 2;
                        yResults.Add(yResult);
                    }
                    else if (particle.emotionCategory == sortedEmotions[1])
                    {
                        float yResult = currentPlanet.position.y;
                        yResults.Add(yResult);
                    }
                    else
                    {
                        float yResult = currentPlanet.position.y - currentPlanet.localScale.y / 2;
                        yResults.Add(yResult);
                    }
                }
                else
                {
                    if (particle.emotionCategory == sortedEmotions[0])
                    {
                        float yResult = currentPlanet.position.y;
                        yResults.Add(yResult);
                    }
                    else if (particle.emotionCategory == sortedEmotions[1])
                    {
                        float yResult = currentPlanet.position.y - currentPlanet.localScale.y / 2;
                        yResults.Add(yResult);

                    }
                }
            }

                
            
        }

        return yResults;
    }

    public void UpdateClusterEmotionsList(List<string> mostProminentEmotions)
    {
        int currentClusterIndex = particleClusterManager.currentPlanetIndex;
        if (currentClusterIndex < clusterProminentColors.Count )//It exists, needs to update
        {
            clusterProminentColors[currentClusterIndex].Clear();
            clusterProminentColors[currentClusterIndex] = mostProminentEmotions;

            
        }
        else
        {
            clusterProminentColors.Add(mostProminentEmotions);

        }

        
    }

    public void CheckAllParticles()
    {
        // Create a dictionary to hold the counts of each emotion
        Dictionary<string, int> emotionCount = new Dictionary<string, int>();

        // Loop through each particle in the list
        foreach (ParticleData particle in ParticleEmotion.particleDataList)
        {
            // Check if the emotion already exists in the dictionary
            if (emotionCount.ContainsKey(particle.emotionCategory))
            {
                // Increment the count if the emotion exists
                emotionCount[particle.emotionCategory]++;
            }
            else
            {
                // Add a new entry for this emotion
                emotionCount.Add(particle.emotionCategory, 1);
            }
        }

        // Print out the results
        
        foreach (KeyValuePair<string, int> emotion in emotionCount)
        {
            Debug.Log($"Emotion: {emotion.Key}, Count: {emotion.Value}");
        }

        CalculateMostProminentEmotionAll(emotionCount);
    }

    public void CalculateMostProminentEmotionAll(Dictionary<string, int> emotionCounts)
    {
        Debug.Log("CALCULATE EMOTIONS ALL");
        bool isMinValuesEqual = false;
        bool isMaxValuesEqual = false;
        bool isAllValuesEqual = false;

        List<int> minEmotionsToMove = new List<int>();
        List<int> medEmotionsToMove = new List<int>();
        List<int> maxEmotionsToMove = new List<int>();
        List<int> particlesInsideClusters = new List<int>();


        // Step 1: Extract keys and values from the dictionary into separate lists
        List<string> emotions = new List<string>(emotionCounts.Keys);
        List<int> counts = new List<int>(emotionCounts.Values);

        // Step 2: Pair the keys and values together
        var pairedList = counts.Select((value, index) => new { Value = value, Key = emotions[index] }).ToList();

        // Step 3: Sort the paired list based on the values (counts)
        pairedList = pairedList.OrderBy(pair => pair.Value).ToList();

        // Step 4: Extract the sorted lists
        List<int> sortedCounts = pairedList.Select(pair => pair.Value).ToList();
        List<string> sortedEmotions = pairedList.Select(pair => pair.Key).ToList();

        // Debug the sorted results
        Debug.Log("Sorted Counts: " + string.Join(", ", sortedCounts));   // Prints: e.g., 2, 2, 5
        Debug.Log("Sorted Emotions: " + string.Join(", ", sortedEmotions)); // Prints: e.g., Negative, Neutral, Positive

        // Step 5: Analyze the sorted counts
        var groupedCounts = sortedCounts.GroupBy(value => value).ToList();


        if (sortedCounts.Count == 1)
        {
            // All values are the same
            isAllValuesEqual = true;
            
        }
        else if (sortedCounts.Count == 2)
        {

            // Compare the first and last groups
            int minValueGroupCount = groupedCounts[0].Count();
            int maxValueGroupCount = groupedCounts[groupedCounts.Count - 1].Count();

            if (sortedCounts[0] == sortedCounts[1])
            {
                isMinValuesEqual = true;
                isMaxValuesEqual = true;
                
            }

            else
            {

                isMinValuesEqual = false;
                isMaxValuesEqual = false;
                
            }
        }

        else
        {

            // Compare the first and last groups
            int minValueGroupCount = groupedCounts[0].Count();
            int maxValueGroupCount = groupedCounts[groupedCounts.Count - 1].Count();

            if (minValueGroupCount > 1)
            {
                isMinValuesEqual = true;
                
                
            }

            if (maxValueGroupCount > 1)
            {
                isMaxValuesEqual = true;
                

            }
        }

        foreach (var group in groupedCounts)
        {
            Debug.Log($"Group Value: {group.Key}, Count: {group.Count()}");
        }

        
        if (particleClusters == null || particleClusters.Count == 0)
        {
            Debug.Log("particleClusters is null or empty.");
        }
        else
        {
            
            Debug.Log("particleClusters ARE NOT EMPTY");
            string result = "";
            for (int i = 0; i < particleClusters.Count; i++)
            {
                var sublist = particleClusters[i];
                result += $"Sublist {i}: {(sublist == null || sublist.Count == 0 ? "Empty" : string.Join(", ", sublist))}\n";
            }
            Debug.Log(result);
        }

        MoveAloneParticles(sortedEmotions, isMinValuesEqual, isMaxValuesEqual, isAllValuesEqual);
        SendYToMovePlanets(sortedEmotions, isMinValuesEqual, isMaxValuesEqual, isAllValuesEqual);
    }

    public void MoveAloneParticles(List<string> sortedEmotions, bool isMinValuesEqual,bool isMaxValuesEqual, bool isAllValuesEqual)
    {
        List<int> minEmotionsToMove = new List<int>();
        List<int> medEmotionsToMove = new List<int>();
        List<int> maxEmotionsToMove = new List<int>();
        List<int> particlesInsideClusters = new List<int>();

        if (sortedEmotions.Count == 3)
        {

            if (isMinValuesEqual)
            {

                if (!isMaxValuesEqual)// To avoid all emotions being equal
                {
                    foreach (ParticleData spawnedParticle in ParticleEmotion.particleDataList)
                    {
                        if (!IsParticleInCluster(particleClusters, spawnedParticle.index))
                        {
                            Debug.Log("NOT INSIDE CLUSTER");
                            if (spawnedParticle.emotionCategory == sortedEmotions[0] || spawnedParticle.emotionCategory == sortedEmotions[1])
                            {

                                minEmotionsToMove.Add(spawnedParticle.index);

                            }
                            else
                            {
                                maxEmotionsToMove.Add(spawnedParticle.index);
                            }
                        }

                        else
                        {
                            particlesInsideClusters.Add(spawnedParticle.index);
                        }



                    }
                    spawnManager.MoveAloneParticles(minEmotionsToMove, 1);// Move them more height
                    spawnManager.MoveAloneParticles(maxEmotionsToMove, 0); // Default unity y
                }
                else// All values are equal with Emotion Count=3
                {
                    foreach (ParticleData spawnedParticle in ParticleEmotion.particleDataList)
                    {
                        if (!IsParticleInCluster(particleClusters, spawnedParticle.index))
                        {
                            Debug.Log("NOT INSIDE CLUSTER");
                            medEmotionsToMove.Add(spawnedParticle.index);
                        }
                        else
                        {
                            particlesInsideClusters.Add(spawnedParticle.index);
                        }
                    }
                    spawnManager.MoveAloneParticles(medEmotionsToMove, 0); // Default unity y
                }

                // Debugging the contents of the lists
                // Debugging the contents of the lists
                Debug.Log("minEmotionsToMove: " + string.Join(", ", minEmotionsToMove));
                Debug.Log("medEmotionsToMove: " + string.Join(", ", maxEmotionsToMove));

            }

            else if (isMaxValuesEqual)
            {
                foreach (ParticleData spawnedParticle in ParticleEmotion.particleDataList)
                {
                    if (!IsParticleInCluster(particleClusters, spawnedParticle.index))
                    {
                        Debug.Log("NOT INSIDE CLUSTER");
                        if (spawnedParticle.emotionCategory == sortedEmotions[1] || spawnedParticle.emotionCategory == sortedEmotions[2])
                        {
                            maxEmotionsToMove.Add(spawnedParticle.index);


                        }
                        else
                        {
                            minEmotionsToMove.Add(spawnedParticle.index);
                        }
                    }
                    else
                    {
                        particlesInsideClusters.Add(spawnedParticle.index);
                    }
                }
                // Debugging the contents of the lists
                Debug.Log("maxEmotionsToMove: " + string.Join(", ", maxEmotionsToMove));
                Debug.Log("medEmotionsToMove: " + string.Join(", ", minEmotionsToMove));

                spawnManager.MoveAloneParticles(maxEmotionsToMove, -1);// Move them less height
                spawnManager.MoveAloneParticles(minEmotionsToMove, 0); // Default unity y

            }

            else if (!isAllValuesEqual)
            {

                // Everything is not equal
                foreach (ParticleData spawnedParticle in ParticleEmotion.particleDataList)
                {
                    if (!IsParticleInCluster(particleClusters, spawnedParticle.index))
                    {
                        Debug.Log("NOT INSIDE CLUSTER");
                        if (spawnedParticle.emotionCategory == sortedEmotions[0])
                        {
                            minEmotionsToMove.Add(spawnedParticle.index);
                        }
                        else if (spawnedParticle.emotionCategory == sortedEmotions[1])
                        {
                            medEmotionsToMove.Add(spawnedParticle.index);
                        }

                        else
                        {
                            if (sortedEmotions.Count > 2)
                            {
                                maxEmotionsToMove.Add(spawnedParticle.index);
                            }
                        }
                    }
                    else
                    {
                        particlesInsideClusters.Add(spawnedParticle.index);
                    }
                }

                // Debugging the contents of the lists
                Debug.Log("minEmotionsToMove: " + string.Join(", ", minEmotionsToMove));
                Debug.Log("medEmotionsToMove: " + string.Join(", ", medEmotionsToMove));
                Debug.Log("maxEmotionsToMove: " + string.Join(", ", maxEmotionsToMove));

                spawnManager.MoveAloneParticles(minEmotionsToMove, 1); //Move them more height
                spawnManager.MoveAloneParticles(medEmotionsToMove, 0);// Default unity y
                spawnManager.MoveAloneParticles(maxEmotionsToMove, -1);//Move them less height
            }
        }

        else if (sortedEmotions.Count == 2)
        {

            if (isMaxValuesEqual || isMinValuesEqual)//  Meaning all are equal
            {
                foreach (ParticleData spawnedParticle in ParticleEmotion.particleDataList)
                {
                    if (!IsParticleInCluster(particleClusters, spawnedParticle.index))
                    {
                        Debug.Log("NOT INSIDE CLUSTER");
                        maxEmotionsToMove.Add(spawnedParticle.index);
                    }
                    else
                    {
                        particlesInsideClusters.Add(spawnedParticle.index);
                    }
                }
                spawnManager.MoveAloneParticles(maxEmotionsToMove, 0);// Default unity y

            }

            else// Two Different Emotions
            {
                foreach (ParticleData spawnedParticle in ParticleEmotion.particleDataList)
                {
                    if (!IsParticleInCluster(particleClusters, spawnedParticle.index))
                    {
                        Debug.Log("NOT INSIDE CLUSTER");
                        if (spawnedParticle.emotionCategory == sortedEmotions[0])
                        {
                            minEmotionsToMove.Add(spawnedParticle.index);
                        }

                        else
                        {
                            maxEmotionsToMove.Add(spawnedParticle.index);
                        }
                    }
                    else
                    {
                        particlesInsideClusters.Add(spawnedParticle.index);
                    }

                }
                spawnManager.MoveAloneParticles(minEmotionsToMove, 1);// Move them more height
                spawnManager.MoveAloneParticles(maxEmotionsToMove, 0);// Default unity y
            }
        }

        

        minEmotionsToMove.Clear();
        medEmotionsToMove.Clear();
        maxEmotionsToMove.Clear();

    }




    private void CreateClustersListOfEmotions(int indexOfCluster, List<string> listOfEmotions, bool IsAllValuesEqual, bool isMinValuesEqual, bool isMaxValuesEqual)
    {
        
        // Ensure that the list has enough elements
        while (totalEmotionsForClusters.Count < indexOfCluster + 1)
        {
            totalEmotionsForClusters.Add(new List<string>()); // Add an empty list if not enough inside list
        }

        // Initialize the cluster with three default values if it's the first time
        if (totalEmotionsForClusters[indexOfCluster].Count == 0)
        {
            Debug.Log($"First initialization on {indexOfCluster}");
            totalEmotionsForClusters[indexOfCluster] = new List<string> { "" }; // Initialize with three default values
        }

        if (listOfEmotions.Count == 1)
        {
            totalEmotionsForClusters[indexOfCluster] = new List<string> { listOfEmotions[0] };
        }

        else if (listOfEmotions.Count == 2)
        {
            totalEmotionsForClusters[indexOfCluster] = new List<string> { listOfEmotions[1] };
            
        }
        else
        {
            if (isMinValuesEqual)
            {
                totalEmotionsForClusters[indexOfCluster] = new List<string> { listOfEmotions[2] };
            }
            else if (isMaxValuesEqual)
            {
                string emotion1 = listOfEmotions[1]; // Second highest
                string emotion2 = listOfEmotions[2]; // Highest

                if ((emotion1 == "Positive" && emotion2 == "Neutral") || (emotion1 == "Neutral" && emotion2 == "Positive"))
                {
                    totalEmotionsForClusters[indexOfCluster] = new List<string> { "Positive" };
                }
                else if ((emotion1 == "Neutral" && emotion2 == "Negative") || (emotion1 == "Negative" && emotion2 == "Neutral"))
                {
                    totalEmotionsForClusters[indexOfCluster] = new List<string> { "Negative" };
                }
                else if ((emotion1 == "Positive" && emotion2 == "Negative") || (emotion1 == "Negative" && emotion2 == "Positive"))
                {
                    totalEmotionsForClusters[indexOfCluster] = new List<string> { "Neutral" };
                }
            }
            else
            {
                totalEmotionsForClusters[indexOfCluster] = new List<string> { listOfEmotions[2] };
            }
        }
        
    }

    public void SendYToMovePlanets(List<string> sortedEmotions, bool isMinValuesEqual, bool isMaxValuesEqual, bool isAllValuesEqual)
    {
        List<int> minPlanetsToMove = new List<int>();
        List<int> medPlanetsToMove = new List<int>();
        List<int> maxPlanetsToMove = new List<int>();
        

        if (sortedEmotions.Count == 3)
        {

            if (isMinValuesEqual)
            {

                if (!isMaxValuesEqual)// To avoid all emotions being equal
                {
                    Debug.Log("MINIMUM VALUES EQUAL");
                    for (int i = 0; i < totalEmotionsForClusters.Count; i++)
                    {
                        var sublist = totalEmotionsForClusters[i];
                        if (sublist.Contains(sortedEmotions[2]))
                        {
                            maxPlanetsToMove.Add(i);
                        }
                        else 
                        {
                            minPlanetsToMove.Add(i);
                        }
                        
                    }
                    particleClusterManager.MovePlanetsBasedOnEmotion(maxPlanetsToMove,0);            
                    particleClusterManager.MovePlanetsBasedOnEmotion(minPlanetsToMove, -1);

                }
                else// All values are equal with Emotion Count=3
                {
                    Debug.Log("ALL VALUES ARE EQUAL");
                    for (int i = 0; i < totalEmotionsForClusters.Count; i++)
                    {
                        var sublist = totalEmotionsForClusters[i];
                        if (sublist.Contains(sortedEmotions[2]))
                        {
                            medPlanetsToMove.Add(i);
                        }
                        
                    }

                    particleClusterManager.MovePlanetsBasedOnEmotion(medPlanetsToMove, 0); // Default unity y
                }

                // Debugging the contents of the lists
                // Debugging the contents of the lists
                Debug.Log("minEmotionsToMove: " + string.Join(", ", minPlanetsToMove));
                Debug.Log("medEmotionsToMove: " + string.Join(", ", maxPlanetsToMove));

            }

            else if (isMaxValuesEqual)
            {
                Debug.Log("MAXIMUM VALUES EQUAL");
                for (int i = 0; i < totalEmotionsForClusters.Count; i++)
                {
                    var sublist = totalEmotionsForClusters[i];
                    if (sublist.Contains(sortedEmotions[2]) || sublist.Contains(sortedEmotions[1]))
                    {
                        maxPlanetsToMove.Add(i);
                    }
                    else
                    {
                        minPlanetsToMove.Add(i);
                    }
                    
                }

                // Debugging the contents of the lists
                Debug.Log("maxEmotionsToMove: " + string.Join(", ", maxPlanetsToMove));
                Debug.Log("medEmotionsToMove: " + string.Join(", ", minPlanetsToMove));

                particleClusterManager.MovePlanetsBasedOnEmotion(maxPlanetsToMove, -1);// Move them less height
                particleClusterManager.MovePlanetsBasedOnEmotion(minPlanetsToMove, 0); // Default unity y

            }

            else if (!isAllValuesEqual)
            {
                Debug.Log("NOTHING IS EQUAL");
                // Everything is not equal
                for (int i = 0; i < totalEmotionsForClusters.Count; i++)
                {
                    var sublist = totalEmotionsForClusters[i];
                    if (sublist.Contains(sortedEmotions[2]))
                    {
                        maxPlanetsToMove.Add(i);
                    }
                    else if (sublist.Contains(sortedEmotions[1]))
                    {
                        medPlanetsToMove.Add(i);
                    }
                    else
                    {
                        minPlanetsToMove.Add(i);
                    }
                }


                // Debugging the contents of the lists
                Debug.Log("minEmotionsToMove: " + string.Join(", ", minPlanetsToMove));
                Debug.Log("medEmotionsToMove: " + string.Join(", ", medPlanetsToMove));
                Debug.Log("maxEmotionsToMove: " + string.Join(", ", maxPlanetsToMove));

                particleClusterManager.MovePlanetsBasedOnEmotion(minPlanetsToMove, 1); //Move them more height
                particleClusterManager.MovePlanetsBasedOnEmotion(medPlanetsToMove, 0);// Default unity y
                particleClusterManager.MovePlanetsBasedOnEmotion(maxPlanetsToMove, -1);//Move them less height
            }
        }

        else if (sortedEmotions.Count == 2)
        {

            if (isMaxValuesEqual || isMinValuesEqual)//  Meaning all are equal
            {
                for (int i = 0; i < totalEmotionsForClusters.Count; i++)
                {              
                    maxPlanetsToMove.Add(i);
    
                }
                particleClusterManager.MovePlanetsBasedOnEmotion(maxPlanetsToMove, 0);// Default unity y

            }

            else// Two Different Emotions
            {
                for (int i = 0; i < totalEmotionsForClusters.Count; i++)
                {
                    var sublist = totalEmotionsForClusters[i];
                    if (sublist.Contains(sortedEmotions[1]))
                    {
                        maxPlanetsToMove.Add(i);
                    }
                    else if (sublist.Contains(sortedEmotions[0]))
                    {
                        minPlanetsToMove.Add(i);
                    }
                    
                }

                particleClusterManager.MovePlanetsBasedOnEmotion(minPlanetsToMove, 1);// Move them more height
                particleClusterManager.MovePlanetsBasedOnEmotion(maxPlanetsToMove, 0);// Default unity y
            }
        }



        minPlanetsToMove.Clear();
        medPlanetsToMove.Clear();
        maxPlanetsToMove.Clear();
    }

    private bool IsParticleInCluster(List<List<int>> clusterList, int particleIndex)
    {
        if (clusterList == null || clusterList.Count == 0)
        {
            Debug.Log("ClusterList is either null or empty.");
            return false;
        }

        // Loop through each sublist in the clusterList
        foreach (var sublist in clusterList)
        {
            // Debug log the contents of the sublist
            Debug.Log($"Sublist contents: {string.Join(", ", sublist)}");

            // Check if the particleIndex exists in the sublist
            if (sublist.Contains(particleIndex))
            {
                Debug.Log($"Found particleIndex {particleIndex} in sublist.");
                return true; // Found the particleIndex in the clusterList
            }
        }

        Debug.Log($"ParticleIndex {particleIndex} not found in any sublist of clusterList.");
        return false; // The particleIndex is not in the clusterList
    }
    

    
}
