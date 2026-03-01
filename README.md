# Sokoban Puzzle Generator
A procedural puzzle generator built in Unity that creates Sokoban-style push block puzzles. This system uses a reverse-time algorithm combined with randomized depth-first generation. This approach mathematically maps a valid solution path, ensuring the vast majority of generated puzzles are highly reliable and fully solvable.
This tool was designed to explore the intersection of algorithmic level design and player psychology, specifically focusing on the difference between apparent complexity (visual noise) and actual difficulty (maneuverability constraints).

**Features**

Reverse-Time Solvability Proof: The generator starts from a solved state and pulls crates backward, guaranteeing that a forward-time player can always push the crates to the goals.
Heuristic Carving: Dynamically routes 1-tile bypass loops around obstacles during generation to prevent mathematically unsolvable deadlocks.
Momentum-Based Hallways: Heavily weights directional generation to create classic, tight, winding Sokoban corridors rather than open, empty rooms.
Decoy Crates (Red Herrings): Injects indistinguishable "garbage" crates into intersections to artificially inflate apparent complexity and test player focus.
Dynamic Difficulty Tagging: Automatically calculates a difficulty score (Easy, Medium, Hard) based on turn counts, crate distance, and decoy density.
The difficulty is calculated using a custom weighted scoring system that evaluates four specific elements of the generated puzzle. It looks at the physical layout of the maze and the cognitive load placed on the player, generating a final score integer.

Here is the formula used in the code:
Score = Turns + (Average Distance * 5) + (Switches * 10) + (Decoys * 15)

**How to Use**

In the Inspector, you can adjust the default grid size and the base puzzle parameters.
Run: Enter Play Mode in Unity.
Iterate: Use the on-screen GUI to regenerate puzzles instantly.
Test Specific Seeds: Uncheck "Use Random Seed" in the UI, type in a specific seed, and click "Regenerate Puzzle" to recall a specific layout. Note that changing parameters will change the original seed.

**Legend**

🔵 Blue Circle: The Player's starting position.

🟩 Green Square: A Goal Switch (the target destination for a crate).

🟨 Yellow Square: A pushable Crate (can be a required puzzle element or a Red Herring decoy).

⬜ White Square: Walkable Floor path.

⬛ Grey Square: Impassable Wall or Pillar.

**Example Outputs:**

Easy:

<img width="333" height="218" alt="image" src="https://github.com/user-attachments/assets/26e74185-9d1f-4e42-8039-cf57ec3473ae" />
<img width="274" height="348" alt="image" src="https://github.com/user-attachments/assets/cb9e94c1-e74c-4149-a277-ef4a3036f0a3" />
<img width="214" height="255" alt="image" src="https://github.com/user-attachments/assets/1c641c21-9b3e-423a-b581-ba42d4bb306a" />


Medium:


<img width="427" height="292" alt="image" src="https://github.com/user-attachments/assets/61a797c8-4dc2-46b6-b5ae-527a95cdbcd1" />
<img width="311" height="389" alt="image" src="https://github.com/user-attachments/assets/9450b775-e278-473a-87f3-05c9c52737a0" />
<img width="468" height="466" alt="image" src="https://github.com/user-attachments/assets/2fae4639-4c3e-4518-b81e-06fde7923383" />

Hard:

<img width="389" height="293" alt="image" src="https://github.com/user-attachments/assets/add28c5c-8b85-480a-a601-a1be873f4766" />
<img width="429" height="368" alt="image" src="https://github.com/user-attachments/assets/1ea8c7ab-bb0d-4088-b4a9-5759579c6272" />
<img width="329" height="464" alt="image" src="https://github.com/user-attachments/assets/58f0b987-94d0-446a-88ea-31e22c6d3f2d" />
<img width="330" height="372" alt="image" src="https://github.com/user-attachments/assets/704d0f23-37c2-49dc-8172-0fcbe11e34fc" />

GIFs of some other generated dungeons:

![Video Project 6](https://github.com/user-attachments/assets/603f61fe-4583-41da-b702-428c6d35ac4e)

![Video Project 7](https://github.com/user-attachments/assets/fde88489-5eb2-4617-aa3c-d133a355084d)




**Design Reflection**
The puzzles consistently feel fair because the reverse-pulling algorithm uses a randomized depth-first generation to mathematically guarantee a valid solution path. To ensure the geometry remains navigable while maintaining tight corridors, I implemented heuristic carving to dynamically route one-tile bypass loops around obstacles during the generation phase. However, visual clarity is intentionally obscured by introducing decoy crates into intersections, creates misdirection as players waste moves on red herrings. The actual difficulty and primary source of frustration stem from maneuvering within these restrictive, carved hallways, actively modeling common player errors where a single incorrect push forces a required crate into an irrecoverable deadlock.
