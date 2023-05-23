using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerNS
{
    public class Player : NetworkBehaviour
    {

        public NetworkVariable<int> choosedColor = new NetworkVariable<int>();
        public List<Material> materials = new List<Material>();
        public NetworkVariable<float> movingDistance;
        public NetworkVariable<int> numberOfColorsAdvantage;

        private PlayerManager playerManager;

        private Rigidbody rb;
        private int colorBeforeAdvantageDisadvantage;
        private float movingDistanceBeforeAdvantageDisadvantage;
        private ulong playerId;


        // ======================================================================================================================= client methods

        // Evitar que elimine a cor 0 (cor anterior) se, por casualidade,
        // fora a elixida aleatoriamente ao spanearse
        private bool firstColorChange = true;

        void Awake() {
            playerManager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
            rb = GetComponent<Rigidbody>();
        }

        void Start() {
            if (IsOwner) {
                SubmitInitialPositionRequestServerRpc();
                //SubmitSetMovingDistanceServerRpc(initialMovingDistance);
                SubmitSetDefaultValuesServerRpc();
                ChangeColor();
            }
        }

        void Update()
        {
            if (IsOwner) {
                if (Input.GetKeyDown(KeyCode.LeftArrow))   SubmitPositionServerRpc(- movingDistance.Value, 0);
                if (Input.GetKeyDown(KeyCode.RightArrow))  SubmitPositionServerRpc(movingDistance.Value, 0);
                if (Input.GetKeyDown(KeyCode.UpArrow))     SubmitPositionServerRpc(0, movingDistance.Value);
                if (Input.GetKeyDown(KeyCode.DownArrow))   SubmitPositionServerRpc(0, -movingDistance.Value);

                if (Input.GetKeyDown(KeyCode.Space)) SubmitPositionJumpingServerRpc();
            }

            GetComponent<MeshRenderer>().material = materials[choosedColor.Value];
        }

        public override void OnNetworkSpawn() {
        }

        public override void OnNetworkDespawn() {
            // Cando un/unha xogador/a abandone a partida
            // eliminamos a cor que tiña
            // da listaxe de cores en uso
            playerManager.RemoveColor(choosedColor.Value);
        }

        public void ChangeColor()
        {
            SubmitChangeColorServerRpc(); 
        }

        private void UnlistPlayer() {
Debug.Log(">>> Velocidade normal: " + movingDistanceBeforeAdvantageDisadvantage);
            SubmitUnlistPlayerServerRpc(playerId, movingDistanceBeforeAdvantageDisadvantage);
        }

        // ======================================================================================================================= ClientRPC

        [ClientRpc]
        public void SetAdvantageDisadvantageClientRpc(ulong playerId, int typeOfAdvantage, float timeToLive, ClientRpcParams clientRpcParams) {
            
            this.playerId = playerId;

            float modifiedMovingDistance;

            // gardar a cor actual
            colorBeforeAdvantageDisadvantage = choosedColor.Value;

            // cambiar cor segundo sexa vantaxe ou desvantaxe
            SubmitSetColorServerRpc(typeOfAdvantage);

            // Gardamos a velocidade actual para restaurala cando remate a vantaxe/desvantaxe
            movingDistanceBeforeAdvantageDisadvantage = movingDistance.Value;

            // Modificar velocidade de movemento
            modifiedMovingDistance = typeOfAdvantage == 0 ? movingDistance.Value * 2 : movingDistance.Value / 2;
            SubmitChangeMovingDistanceServerRpc(modifiedMovingDistance);

Debug.Log($">>>>>> Comeza vantaxe/desvantaxe {typeOfAdvantage} do xogador {playerId}, nova velocidade {movingDistance.Value} durante {timeToLive} seg.");
            
            // Invocar ServRpc que elimine ao xogador da lista de xogadores modificados pasado o tempo indicado en timeToLive
            Invoke("UnlistPlayer", timeToLive);
        }

        // ======================================================================================================================= ServerRPC

        [ServerRpc]
        void SubmitUnlistPlayerServerRpc(ulong playerId, float oldMovingDistance) {
            SubmitSetColorServerRpc(colorBeforeAdvantageDisadvantage);
            movingDistance.Value = oldMovingDistance;
Debug.Log($">>>>>>>> Fin vantaxe/desvantaxe do xogador {playerId}; nova velocidade {movingDistance.Value}");
            playerManager.playersWithSpeedModified.Remove(playerId);
        }

        [ServerRpc]
        void SubmitChangeColorServerRpc(){
            
            int newColor = -1;  //obrigar a entrar no bucle while
            int oldColor = choosedColor.Value;

            // Escollemos unha cor libre aleatoriamente
            while (newColor < 0)  {
                newColor = Random.Range(numberOfColorsAdvantage.Value, materials.Count);
                if (playerManager.usedColors.Contains(newColor)) {
                    newColor = -1;
                }
            }

            playerManager.AddColor(newColor);
            // Cando mude de cor, eliminamos a cor anterior
            // da listaxe de cores en uso. Agás se eliximos
            // a cor por primeira vez
            if (! firstColorChange) {
                playerManager.RemoveColor(oldColor);
            } else {                
                firstColorChange = false;
            }
            choosedColor.Value = newColor;

        }

        [ServerRpc]
        void SubmitChangeMovingDistanceServerRpc(float distance) {            
            movingDistance.Value = distance;
        }

        [ServerRpc]
        void SubmitSetColorServerRpc(int newColor) {
            choosedColor.Value = newColor;
        }

        [ServerRpc]
        void SubmitSetDefaultValuesServerRpc() {
            movingDistance.Value = 0.4f;
            numberOfColorsAdvantage.Value = 2;
        }

        [ServerRpc]
        void SubmitInitialPositionRequestServerRpc()
        {
            transform.position = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        }

        [ServerRpc]
        void SubmitPositionServerRpc(float moveLeftRight, float moveBackForward){
            transform.position = new Vector3(transform.position.x + moveLeftRight, transform.position.y, transform.position.z + moveBackForward);
        }

        [ServerRpc]
        void SubmitPositionJumpingServerRpc() {
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);
        }
    }
}