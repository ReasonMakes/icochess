# icochess
3D chess on a pentakis icosidodecahedron

Placeholder models for the chess pieces by dhruvin_doctor
https://free3d.com/3d-model/chess-19650.html

CONTROLS:
Left-click hold and drag - 	orbit the camera around the pentakis icosidodecahedron
Left-click on a tile - 		select that tile
Right-click on a tile - 	move the piece on the tile you have selected to that tile/capture that enemy piece
Scroll up/down -			move the camera closer/farther from the pentakis icosidodecahedron
F11 -						toggle fullscreen

PIECE BEHAVIOURS:
Pawns -			Pawns move/capture one edge away. There is no en passant. Pawns can move "backwards"
Knights -		Knights move/capture one side corner away, ignoring obstructions in the way
Bishops -		Bishops move/capture infinitely from edges to direct corners and vice versa along one direction
Rooks -			Rooks move/capture infinitely from edge to edge along one direction
Kings - 		Kings move/capture one edge away
Queens -		Queens have all the valid moves of a rook and a bishop at the same time

No piece can move through a tile occupied by another piece except the knight.
Pieces capture enemy pieces by moving to replace the enemy piece on that tile.
After you move a piece, your turn ends and it becomes the opponent's turn.

CHECK AND GAME OVER CONDITIONS:
Your king is said to be in check if on the opponent's next turn your king could be captured if the position remained as it is.
If you are in check, you must move your king out of check, block the check, or eliminate the check through a capture.
If you cannot save your king from check on your turn then it is said to be checkmate - the game is over and you lose.
You cannot move your king into check.

When it is your turn you must make a move - you cannot skip your turn.
If there are no legal moves (ex: your king would be in check), the game is said to be a stalemate - the game is a draw and no one wins.

TILE RELATIONSHIPS:
Tiles are said to be neighbours when they shared one or more vertices.
Each tile has three distinct types of neighbours.

Edges -				share two vertices
Side corners - 		share one vertex and are mutual neighbours with an edge neighbour
Direct corners -	share one vertex and all vertex positions are symmetrical

All tiles that share one vertex but don't have mutual edge neighbours are direct corner neighbours.
All tiles have exactly 6 side corners, and either 2 or 3 direct corners.

FUNDAMENTAL PROPERTIES OF THE PENTAKIS ICOSIDODECAHEDRON:
The pentakis icosidodecahedron has patterns of pentagons and hexagons, similar to a soccer ball.
The pentakis icosidodecahedron is the dual (the vertices of one correspond to the faces of the other) of a soccer ball (truncated rhombic triacontahedron/chamfered dodecahedron).
The pentakis icosidodecahedron is the result of subdividing an icosahedron - one of the five Platonic solids (convex, regular polyhedron), and the one with the most faces (20).
The pentakis icosidodecahedron has 80 triangle faces - 20 equilateral and 60 isosceles