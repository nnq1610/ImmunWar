using System;
using System.Collections.Generic;
using System.Linq;
using ImmunWar.Battle.Placement;
using UnityEngine;

namespace ImmunWar.Battle.Economy
{
    public enum ATPTransactionType
    {
        PlacementCost,
        StrategicNodeBonus,
        EnergyGeneration, 
        WaveReward,
        FeverBonus
    }

    [Serializable]
    public struct ATPTransaction
    {
        public string TransactionId;
        public int Amount;
        public ATPTransactionType Type;
        public GridPosition? Position;
        public string DefenderId;
        public int Timestamp;
    }

    [Serializable]
    public struct PlacementOrder
    {
        public string DefenderId;
        public GridPosition Position;
        public int RequiredATP;
    }

    /// <summary>
    /// Core ATP economy management, tracking transactions, placement costs, and queuing.
    /// Implements Task 8 of the immunwar-placement-ui-redesign feature.
    /// </summary>
    public class EnhancedATPSystem
    {
        private int _currentATP;
        private int _projectedGenerationRate;
        private readonly List<ATPTransaction> _transactions;
        private readonly Queue<PlacementOrder> _placementQueue;
        private readonly GridSystem _gridSystem;

        public int Current => _currentATP;
        
        public EnhancedATPSystem(int initialATP, GridSystem gridSystem = null)
        {
            _currentATP = initialATP;
            _gridSystem = gridSystem;
            _transactions = new List<ATPTransaction>();
            _placementQueue = new Queue<PlacementOrder>();
        }

        public bool CanAfford(int cost, GridPosition? position = null)
        {
            return _currentATP >= cost;
        }

        public int GetPlacementCost(string defenderId, GridPosition position)
        {
            int baseCost = GetBaseCost(defenderId);

            if (_gridSystem != null && _gridSystem.TryGetNode(position, out var node) && node.Type == NodeType.Strategic)
            {
                return Mathf.RoundToInt(baseCost * 1.25f);
            }

            return baseCost;
        }

        public void ProcessPlacement(string defenderId, GridPosition position)
        {
            int cost = GetPlacementCost(defenderId, position);
            if (CanAfford(cost))
            {
                _currentATP -= cost;
                RecordTransaction(-cost, ATPTransactionType.PlacementCost, defenderId, position);
            }
        }

        public void ProcessStrategicNodeBonus(GridPosition position)
        {
            if (_gridSystem != null && _gridSystem.TryGetNode(position, out var node) && node.Type == NodeType.Strategic)
            {
                // Give a bonus ATP generation for Energy Cells placed on strategic nodes
                float bonus = node.StrategyData.ATPGenerationBonus;
                if (bonus > 0)
                {
                    int amount = Mathf.RoundToInt(bonus * 10); // arbitrary scaling for bonus
                    _currentATP += amount;
                    RecordTransaction(amount, ATPTransactionType.StrategicNodeBonus, "EnergyBonus", position);
                }
            }
        }

        private void RecordTransaction(int amount, ATPTransactionType type, string defenderId = null, GridPosition? pos = null)
        {
            var transaction = new ATPTransaction
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                Amount = amount,
                Type = type,
                DefenderId = defenderId,
                Position = pos,
                Timestamp = Mathf.RoundToInt(Time.time)
            };
            _transactions.Add(transaction);
        }

        public void QueuePlacement(PlacementOrder order)
        {
            _placementQueue.Enqueue(order);
        }

        public void ProcessQueuedPlacements()
        {
            while (_placementQueue.Count > 0)
            {
                var peekOrder = _placementQueue.Peek();
                int cost = GetPlacementCost(peekOrder.DefenderId, peekOrder.Position);
                
                if (CanAfford(cost))
                {
                    var order = _placementQueue.Dequeue();
                    ProcessPlacement(order.DefenderId, order.Position);
                }
                else
                {
                    // Cannot afford the next item in the queue, stop processing
                    break;
                }
            }
        }

        public IEnumerable<PlacementOrder> GetPendingQueue()
        {
            return _placementQueue.AsEnumerable();
        }

        public void UpdateProjectedGeneration(int generationRate)
        {
            _projectedGenerationRate = generationRate;
        }

        public int GetProjectedATP()
        {
            // E.g. Projected ATP next cycle
            return _currentATP + _projectedGenerationRate;
        }
        
        public IEnumerable<string> SuggestAlternativeDefenders(int maxCost)
        {
            // Simplified suggestion logic based on base cost
            var affordable = new List<string>();
            string[] knownTypes = { "Macrophage", "T-Cell", "B-Cell", "NK", "Energy", "Platelet" };
            foreach (var type in knownTypes)
            {
                if (GetBaseCost(type) <= maxCost)
                {
                    affordable.Add(type);
                }
            }
            return affordable;
        }

        private int GetBaseCost(string defenderId)
        {
            return defenderId.ToLower() switch
            {
                var id when id.Contains("macrophage") => 75,
                var id when id.Contains("tcell") => 100,
                var id when id.Contains("bcell") => 125,
                var id when id.Contains("nk") => 150,
                var id when id.Contains("platelet") => 90,
                var id when id.Contains("energy") => 200,
                _ => 100
            };
        }
    }
}
