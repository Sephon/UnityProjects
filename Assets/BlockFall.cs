using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BlockFall : MonoBehaviour {
    public Vector3 AreaPosition = Vector3.zero;
    public Vector3 AreaSize = new Vector3(5, 5, 5);
    public Vector3 ItemSize = Vector3.one;
    public float BlockFallSpeed = 0.5f;
    public float GameOverExplosionForce = 100f;
    public TextMesh TextOverlay;
    public Light SpotLight;

    public event BlockItemWasGroundedHandler BlockItemWasGrounded;
    public delegate void BlockItemWasGroundedHandler(BlockFallItem block);

    private AudioClip _plop;
    protected bool SpawnNewBlockFall = false;
    private const string BlockName = "FallingBlock";
    private Material blockMaterialOrange;
    private Material blockMaterialBlue;
    private Material blockMaterialWater;
    private Material blockMaterialRed;
    private Material blockMaterialGreen;
    private Material WallAreaBoundsMaterial;
    private List<BlockFallItem> BlockItems = new List<BlockFallItem>();
    private List<GameObject> WallBoundaries = new List<GameObject>();
    private System.Random Rnd;
    private bool IsGameOver = false;

    public enum MoveDirection
    {
        Left = 0,
        Right = 1,
        Forward = 2,
        Back = 3,
        Up = 4,
        Down = 5,
        Falling
    };

    // Use this for initialization
    void Start()
    {
        blockMaterialRed = Resources.Load<Material>("proto_red");
        blockMaterialGreen = Resources.Load<Material>("proto_green");
        blockMaterialOrange = Resources.Load<Material>("proto_orange");
        blockMaterialBlue = Resources.Load<Material>("proto_blue");
        blockMaterialWater = Resources.Load<Material>("proto_water");
        WallAreaBoundsMaterial = Resources.Load<Material>("WallAreaBounds");
        _plop  = Resources.Load<AudioClip>("plop");
        BlockItemWasGrounded += new BlockItemWasGroundedHandler((block) =>
        {
            AudioSource.PlayClipAtPoint(_plop, block.Item.transform.position, 50f);
            SpawnNewBlockFall = true;
        });
        Rnd = new System.Random();

        DrawAreaBounds();
    }

    // Update is called once per frame
    void Update()
    {
        HandleKeyEvents();

        CheckBlockSpawn();
        CheckBlockCollision();
        CleanupDeadBlocks();

        HandleDirectedSpotLight();
    }

    private void HandleDirectedSpotLight()
    {
        var block = BlockItems.FirstOrDefault((b) => b.IsAlive && !b.IsGrounded);

        if(block != null && SpotLight != null)
            SpotLight.transform.position = new Vector3(block.Item.transform.position.x, AreaPosition.y + AreaSize.y, block.Item.transform.position.z);
    }

    private void DrawAreaBounds()
    {
        Vector3 margin = new Vector3(0.001f, 0f, 0.001f);
        float thickness = 0.01f;
        var southWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var southScale = new Vector3(AreaSize.x, AreaSize.y, thickness);
        southWall.transform.localScale = southScale;
        southWall.transform.position = (AreaPosition - margin) + (southScale / 2f);
        southWall.GetComponent<Renderer>().material = WallAreaBoundsMaterial;

        var westWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var westScale = new Vector3(thickness, AreaSize.y, AreaSize.z);
        westWall.transform.localScale = westScale;
        westWall.transform.position = (AreaPosition - margin) + (westScale / 2f);
        westWall.GetComponent<Renderer>().material = WallAreaBoundsMaterial;

        var eastWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var eastScale = new Vector3(thickness, AreaSize.y, AreaSize.z);
        eastWall.transform.localScale = eastScale;
        eastWall.transform.position = (AreaPosition + margin) + (eastScale / 2f) + new Vector3(AreaSize.x,0f,0f);
        eastWall.GetComponent<Renderer>().material = WallAreaBoundsMaterial;

        var northWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var nortScale = new Vector3(AreaSize.x, AreaSize.y, thickness);
        northWall.transform.localScale = nortScale;
        northWall.transform.position = (AreaPosition + margin) + (southScale / 2f) + new Vector3(0f, 0f, AreaSize.z);
        northWall.GetComponent<Renderer>().material = WallAreaBoundsMaterial;

        WallBoundaries.AddRange(new GameObject[] { southWall, westWall, eastWall, northWall });
    }

    private void SpawnRandomBlock()
    {       
        var intVal = Rnd.Next(0, System.Enum.GetValues(typeof(BlockFallItem.BlockType)).Length);
        SpawnBlock((BlockFallItem.BlockType) intVal);
    }

    private void SpawnBlock(BlockFallItem.BlockType type)
    {
        if (IsGameOver)
            return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        switch(type)
        {
            case BlockFallItem.BlockType.Block1:
                go.transform.localScale = ItemSize * 0.99f;
                break;
            case BlockFallItem.BlockType.Squiggly:
                go.transform.localScale = ItemSize ;
                break;
            case BlockFallItem.BlockType.Block3Line:
                go.transform.localScale = new Vector3(ItemSize.x * 3, ItemSize.y, ItemSize.z);
                break;
            case BlockFallItem.BlockType.Block2Line:
                go.transform.localScale = new Vector3(ItemSize.x * 2, ItemSize.y, ItemSize.z);
                break;

        }
        go.transform.localScale *= 0.99f; //Shrink a tiny bit so we don't collide on fall.
        go.transform.position = AreaPosition + new Vector3((go.transform.localScale.x / 2f), 
            AreaSize.y + (ItemSize.y /2f),
            (ItemSize.z / 2f));
        go.tag = BlockName;

        var block = ActivateBlock(go, type);

        BlockItems.Add(block);
    }

    private BlockFallItem ActivateBlock(GameObject go, BlockFallItem.BlockType type)
    {
        var physics = go.AddComponent<Rigidbody>();
        physics.isKinematic = true;
        var collider = go.AddComponent<BoxCollider>();        
                     
        var renderer = go.GetComponent<Renderer>();
        switch(type)
        {
            case BlockFallItem.BlockType.Block1:
                renderer.material = blockMaterialOrange;
                break;
            case BlockFallItem.BlockType.Squiggly:
                renderer.material = blockMaterialOrange;
                break;
            case BlockFallItem.BlockType.Block3Line:
                renderer.material = blockMaterialRed;
                break;
            case BlockFallItem.BlockType.Block2Line:
                renderer.material = blockMaterialGreen;
                break;
        }
        
        return new BlockFallItem(go, collider, this, type);
    }

    private bool AreAllGrounded()
    {
        return !BlockItems.Any((b) => !b.IsGrounded);
    }

    private void NewtoniateAllItems()
    {
        foreach(var block in BlockItems)
        {
            var body = block.Item.GetComponent<Rigidbody>();
            body.useGravity = true;
            body.isKinematic = false;
        }

        foreach(var wall in WallBoundaries)
        {
            var rb = wall.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;

            var col = wall.AddComponent<BoxCollider>();            
        }
    }

    private void ExplodeBoard()
    {
        NewtoniateAllItems();

        var radius = AreaSize.y;
        var explosionCenter = AreaPosition + new Vector3(AreaSize.x / 2f, 0f, AreaSize.z / 2f);
        foreach (var collider in Physics.OverlapSphere(explosionCenter, radius))
        {
            var rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(GameOverExplosionForce, explosionCenter, radius);
        }
    }

    private void CheckForCompletedBoards()
    {
        var rowScoreList = new Dictionary<int, float>();
        var completedScore = (AreaSize.x / (ItemSize.x) ) * (AreaSize.z / (ItemSize.z));
        foreach(var block in BlockItems)
        {
            float currScore = 0;
            if (rowScoreList.TryGetValue(block.GetRow(), out currScore))
                rowScoreList[block.GetRow()] = currScore + block.GetScore();
            else
                rowScoreList.Add(block.GetRow(), block.GetScore());            
        }

        for(int row = 0; row < AreaSize.y; row++)
        {
            float rowScore = 0;
            rowScoreList.TryGetValue(row, out rowScore);
            if(rowScore >= completedScore)
                CompleteBoard(row);
        }
    }

    private void CompleteBoard(int completedRow)
    {

        var completedBlocks = BlockItems.Where((b) => b.GetRow().Equals(completedRow));
        KillBlocks(completedBlocks);
    }

    private void CleanupDeadBlocks()
    {
        bool didCleanup = false;
        for (var i = BlockItems.Count - 1; i >= 0; i--)
        {
            var block = BlockItems[i];
            if(block.Cleanup())
            {
                BlockItems.RemoveAt(i);
                didCleanup = true;
            }
        }

        if(didCleanup)
            UnGroundAllBlocks(); // Makes blocks above fall down
    }

    private void KillBlocks(IEnumerable<BlockFallItem> blocks)
    {
        blocks.ToList().ForEach((b) => b.Kill());
    }

    private void UnGroundAllBlocks()
    {
        foreach(var block in BlockItems.OrderBy((b) => b.Item.transform.position.y))
        {
            if(block.IsAlive && block.IsGrounded && block.Item.transform.position.y > AreaPosition.y)
                block.IsGrounded = false;
        }
    }

    private void HandleKeyEvents()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            SpawnBlock(BlockFallItem.BlockType.Block3Line);

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            DirectBlock(MoveDirection.Left);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            DirectBlock(MoveDirection.Right);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            DirectBlock(MoveDirection.Back);
        if (Input.GetKeyDown(KeyCode.UpArrow))
            DirectBlock(MoveDirection.Forward);

        if (Input.GetKeyDown(KeyCode.LeftControl))
            RotateBlock();
    }

    public void DirectBlock(MoveDirection direction)
    {
        foreach (var block in BlockItems.Where((block) => block.IsAlive && !block.IsGrounded))
        {
            block.Move(direction);
        }
    }

    public void RotateBlock()
    {
        foreach (var block in BlockItems.Where((block) => block.IsAlive && !block.IsGrounded))
        {
            if (block.IsAlive && !block.IsGrounded)
                block.Rotate();
        }
    }


    protected void CheckBlockSpawn()
    {
        if (SpawnNewBlockFall)
        {
            SpawnRandomBlock();
            SpawnNewBlockFall = false;
        }
    }

    protected void CheckBlockCollision()
    {
        foreach(var block in BlockItems.OrderBy((b) => b.Item.transform.position.y))
        {
            if (!block.IsGrounded) // && Time.time - block.LastFallInterval > BlockTickIntervalTickTime)
                block.Move(MoveDirection.Falling);

            foreach (var block2 in BlockItems.Where((b) => b.IsAlive && b.IsGrounded))
            {
                var nextPosition = block.GetPositionAfterMove(MoveDirection.Falling);
                var bounds = block.Collider.bounds;
                bounds.center = new Vector3(bounds.center.x + (nextPosition.x - block.Item.transform.position.x), bounds.center.y + (nextPosition.y - block.Item.transform.position.y), bounds.center.z + (nextPosition.z - block.Item.transform.position.z));
                var bounds2 = block2.Collider.bounds;
                if (!block.Equals(block2) &&  !block.IsGrounded && bounds.Intersects(bounds2))
                    block.GroundItem(bounds2.max.y + bounds2.extents.y);
            }
        }
    }    

    public class BlockFallItem 
    {
        public enum BlockType
        {
            Block1 = 0,
            Squiggly = 1,
            Block3Line = 2,
            Block2Line = 3
        };

        public BlockType Type;
        public GameObject Item;
        public BlockFall Parent;
        public BoxCollider Collider;

        public bool IsGrounded = false;
        public bool IsAlive = true;
        public bool HasLanded = false;

        private Renderer renderer;
        private float LastFallInterval = 0;
        private bool IsCompleted = false;
        private int _attachedOnRow = -1;
        private float _killTime;
        private float _deathAnimationTime = 0.5f;

        public void Complete()
        {
            IsCompleted = true;
            this.renderer.material = Parent.blockMaterialGreen;
        }

        public int GetRow()
        {
            return _attachedOnRow;
        }

        public int GetScore()
        {
            switch(Type)
            {
                case BlockType.Block1:
                    return 1;
                case BlockType.Block2Line:
                    return 2;
                case BlockType.Block3Line:
                    return 3;
                case BlockType.Squiggly:
                    return 1;

            }

            return 0;
        }

        public BlockFallItem(GameObject item, BoxCollider collider, BlockFall parent, BlockType type)
        {
            this.Item = item;
            this.Parent = parent;
            this.Collider = collider;
            this.renderer = item.GetComponent<Renderer>();
            this.Type = type;
        }

        public void Move(MoveDirection direction)
        {
            var nextPos = GetPositionAfterMove(direction);
            if (direction != MoveDirection.Falling && IsOutOfBounds(nextPos))
                return;

            if (!IsGrounded)
                Item.transform.position = nextPos;

            if (!IsGrounded && Item.transform.position.y - Collider.bounds.extents.y <= Parent.AreaPosition.y)
                GroundItem(Parent.AreaPosition.y + Collider.bounds.extents.y);
        }

        public void Rotate()
        {
            Item.transform.RotateAround(Item.transform.position, Vector3.up, 90f);
            var xCalc = (Item.transform.localScale.x - Parent.ItemSize.x) / 2f;
            switch (Type)
            {
                case BlockType.Block2Line:
                    Item.transform.position -= new Vector3(xCalc, 0f, -xCalc);
                    break;
                case BlockType.Block3Line:
                    Item.transform.position -= new Vector3(xCalc, 0f, -xCalc);
                    break;
            }
            //var rotation = Item.transform.localRotation;
            //rotation *= Quaternion.Euler(0, 90, 0);
            //Item.transform.localRotation = rotation;
        }

        private bool IsOutOfBounds(Vector3 position)
        {
            var areaBounds = new Bounds(Parent.AreaPosition + (Parent.AreaSize / 2f), Parent.AreaSize);
            var areaBoundsEncapsulated = new Bounds(Parent.AreaPosition + (Parent.AreaSize / 2f), Parent.AreaSize);
            areaBoundsEncapsulated.Encapsulate(GetBlockBounds(position));
            if (areaBounds != areaBoundsEncapsulated)
                return true;

            return false;
        }

        private Bounds GetBlockBounds(Vector3 position)
        {
            return new Bounds(position, Item.transform.localScale);
        }

        public void Kill()
        {
            IsAlive = false;
            _killTime = Time.time;
        }

        public bool Cleanup()
        {
            if(!IsAlive)
            {
                //Shrink
                this.Item.transform.localScale *= 0.9f;
            }

            if(!IsAlive && Time.time - _killTime > _deathAnimationTime)
            {
                Destroy(this.Item);
                return true;
            }

            return false;
        }

        public Vector3 GetPositionAfterMove(MoveDirection direction)
        {
            Vector3 scalar = Vector3.zero;
            Vector3 moveAmount = Vector3.zero;
            switch (direction)
            {
                case (MoveDirection.Left):
                    moveAmount = Vector3.Scale(Parent.ItemSize, -Item.transform.right);
                    break;
                case (MoveDirection.Right):
                    moveAmount = Vector3.Scale(Parent.ItemSize, Item.transform.right);
                    break;
                case (MoveDirection.Forward):
                    moveAmount = Vector3.Scale(Parent.ItemSize, Item.transform.forward);
                    break;
                case (MoveDirection.Back):
                    moveAmount = Vector3.Scale(Parent.ItemSize, -Item.transform.forward);
                    break;
                case MoveDirection.Down:
                    moveAmount = Vector3.Scale(Parent.ItemSize, -Item.transform.up);                    
                    break;
                case MoveDirection.Falling:
                    moveAmount = (-Item.transform.up * Parent.BlockFallSpeed) * Time.deltaTime;
                    LastFallInterval = Time.time;
                    break;
                case MoveDirection.Up:
                    moveAmount = Vector3.Scale(Parent.ItemSize, Item.transform.up);
                    break;
            }
            
            return Item.transform.position + moveAmount;
        }

        public void GroundItem(float targetY)
        {
            int groundedOnRow = (int) Mathf.Floor((targetY - Parent.AreaPosition.y) / Parent.ItemSize.y);
            _attachedOnRow = groundedOnRow;

            //Parent.TextOverlay.text = string.Format("Last block grounded on Row {0}", groundedOnRow);

            var light = this.Item.GetComponent<Light>();
            if (light != null)
                Destroy(light);

            if (!IsAlive)
                return;

            Parent.CheckForCompletedBoards();

            IsGrounded = true;
            Item.transform.position = new Vector3(Item.transform.position.x, targetY, Item.transform.position.z);
            
            if(!HasLanded)
            {
                if (!Parent.IsGameOver && IsOutOfBounds(Item.transform.position))
                {
                    Parent.IsGameOver = true;
                    Parent.ExplodeBoard();
                }

                Parent.BlockItemWasGrounded(this);
                HasLanded = true;
            }
        }
    }
}
