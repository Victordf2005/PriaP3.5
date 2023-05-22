using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PlayerNS
{
    public class PlayerManager : NetworkBehaviour
    {

        public NetworkList<int> usedColors;        
        public NetworkList<ulong> playersWithSpeedModified;
        
        private static float settedAdvantageRate = 0.5f;
        private static ulong selectedPlayer;
        private static bool isPlayerSelected;
        private static float[] timeAdvantageDisadvantage = {10f, 20f};

        private static bool coroutineLaunched;

        void Awake() {
            usedColors = new NetworkList<int>();
            playersWithSpeedModified = new NetworkList<ulong>();
            coroutineLaunched = false;
        }
        
        public override void OnNetworkSpawn() {

            // Só o servidor pode otorgar premio ou castigo
            if (NetworkManager.Singleton.IsServer) {
                StartCoroutine(CheckForAdvantages());
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();

                if (IsHost || IsClient) SubmitChangeColor();

            }
            GUILayout.EndArea();
        }

        static void StartButtons()
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }

        static void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ?
                "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }

        static void SubmitChangeColor()
        {
            if (GUILayout.Button("Change color"))
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<Player>();
                player.ChangeColor();
            }
        }

        private IEnumerator CheckForAdvantages() {
           
            int typeOfAdvantage;
            ClientRpcParams clientRpcParams;
            Player p;

            while (true) {

                // Só lanzamos unha vantaxe/desvantaxe segundo unha porcentaxe establecida en SettedAdvantageRate
                // sempre e cando haxa players "libres" (cando o número de clientes é maior ca de xogadores con vantaxe ou desvantaxe)
                if (Random.Range(0, 1) < settedAdvantageRate && NetworkManager.Singleton.ConnectedClientsIds.Count > playersWithSpeedModified.Count) {

                    // Buscamos un xogador "libre"
                    isPlayerSelected = false;
                    selectedPlayer = 0;    // obrigamos a entrar no bucle
                    
                    while ( ! isPlayerSelected ) {
                        selectedPlayer = (ulong) Random.Range(0, NetworkManager.Singleton.ConnectedClientsIds.Count);
                        if (! playersWithSpeedModified.Contains(selectedPlayer)) isPlayerSelected = true;
                    }

                    // seleccionamos tipo (vantaxe ou desvantaxe), segundo os elementos do array de tempos de cada unha;
                    typeOfAdvantage = Random.Range(0, timeAdvantageDisadvantage.Length);                

                    // Xa temos xogador e tipo (vantaxe/desvantaxe) con tempo incluido
                    // Preparamos a chamada ao ClienteRpc
                    clientRpcParams = new ClientRpcParams {
                        Send = new ClientRpcSendParams {
                            TargetClientIds = new ulong[]{selectedPlayer}
                        }
                    };

                    // engadimos o player á listaxe de players con vantaxe/desvantaxe
                    playersWithSpeedModified.Add(selectedPlayer);
Debug.Log("Xogador seleccionado: " + selectedPlayer);
                    // chamada a clientRpc
                    p = NetworkManager.Singleton.ConnectedClientsList[(int) selectedPlayer].PlayerObject.GetComponent<Player>();
                    p.SetAdvantageDisadvantageClientRpc(selectedPlayer, typeOfAdvantage, timeAdvantageDisadvantage[typeOfAdvantage], clientRpcParams);
                    
                }

                yield return new WaitForSeconds(1f);

            }
        }
        
        public void AddColor(int color) {            
            usedColors.Add(color);
        }

        public void RemoveColor(int color) {
            usedColors.Remove(color);
        }

        

    }
}