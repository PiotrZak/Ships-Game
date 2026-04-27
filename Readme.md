BattleShips game from scratch:

4 Game modes:


- <b>Pvp</b> -> Player vs Player
- <b>PvA</b> -> Player vs Ai (Smart AI)
- <b>AvA</b> -> Ai vs Ai (Smart AI vs Smart AI)
- <b>Strait of Ormuz</b> -> Player vs Smart AI with a limited fleet in a strategic naval scenario

![Screen.png](Screen.png)

## Smart AI (ML Algorithm)

The AI opponent uses a **Probability-Density Heat-Map** algorithm inspired by Bayesian inference and tabular reinforcement learning:

1. **Heat-Map Generation** – For every remaining ship size the AI enumerates all valid horizontal and vertical placements on the board. A placement is valid when none of its cells is a confirmed miss. Each cell in a valid placement earns `+1` probability score, so cells that can be covered by more ship configurations score higher.
2. **Zero-out fired cells** – Cells that have already been shot (hit or miss) are excluded from consideration.
3. **Target Mode (Hunt-and-Finish)** – When one or more hits exist whose ship has not been sunk yet, the four direct neighbours of those hit cells receive a `×3` probability boost. This causes the AI to focus on finishing off a wounded ship before hunting for a new one.
4. **Shot selection** – The cell with the highest heat-map score is chosen as the next shot.

This is significantly smarter than pure random fire: it exploits the constraint that ships occupy contiguous cells and aggressively follows up on hits.

## Strait of Ormuz Mode

A limited-fleet scenario set in the world's most strategically important waterway.

- **Fleet**: Destroyer · Submarine · Cruiser (3 ships instead of the standard 5)
- **Opponent**: Smart AI (probability heat-map algorithm)
- **Objective**: Sink the enemy fleet before they sink yours

Ideas to do:

- Ranking for person groups per table
- Improve AI targeting (directional follow-up after consecutive hits)
- Omit the stupid action in AI vs AI mode:

    - Hey its already missed, don't waste energy!
    - Its your ship already attacked!
    - Its enemy ship already attacked"
