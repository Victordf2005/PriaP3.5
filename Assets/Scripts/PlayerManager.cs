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
        private static int typeOfVantaxe;
        private static float[] timeAdvantageDisadvantage = {10f, 20f};

        void Awake() {
            usedColors = new NetworkList<int>();
            playersWithSpeedModified = new NetworkList<ulong>();
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

                if (IsServer) {
                    CheckForAdvantages();
                }
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

        static void CheckForAdvantages() {

            // Só lanzamos unha vantaxe/desvantaxe segundo unha porcentaxe establecida en SettedAdvantageRate
            // sempre e cando haxa players "libres" (cando o número de clientes é maior ca de xogadores con vantaxe ou desvantaxe)
            if (Random.Range(0, 1) < settedAdvantageRate && NetworkManager.Singleton.connectedClientesIds.Count > playersWithSpeedModified.Count) {

                // Buscamos un xogador "libre"
                selectedPlayer = -1;    // obrigamos a entrar no bucle
                while (selectedPlayer <0 ) {
                    selectedPlayer = Random.Range(0, NetworkManager.Singleton.connectedClientesIds.Count);
                    if (playersWithSpeedModified.Contains(selectedPlayer)) selectedPlayer = -1;
                }

                // seleccionamos tipo (vantaxe ou desvantaxe), segundo os elementos do array de tempos de cada unha;
                int typeOfVantaxe = Random.Range(0, timeAdvantageDisadvantage.Length);                

                // Xa temos xogador e tipo (vantaxe/desvantaxe) con tempo incluido
                // Preparamos a chamada ao ClienteRpc
                ClientRpcParams clientRpcParams = new ClientRpcParams {
                    Send = new ClientRpcParams {
                        TargetClientIds = new ulong[]{selectedPlayer}
                    }
                };

                // engadimos o player á listaxe de players con vantaxe/desvantaxe
                playersWithSpeedModified.Add(selectedPlayer);

                // chamada a clientRpc
                SetAdvantageDisadvantageClientRpc(timeAdvantageDisadvantage[typeOfVantaxe], clientRpcParams);

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