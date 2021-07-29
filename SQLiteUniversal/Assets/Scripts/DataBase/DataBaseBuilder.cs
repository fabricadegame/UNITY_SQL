using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Mono.Data.Sqlite;
using System.IO;
using System;

public class DataBaseBuilder : MonoBehaviour
{
    public string DataBaseName;
    protected string DataBasePath;
    protected SqliteConnection Connection => new SqliteConnection($"Data Source = {DataBasePath};");

    private void Awake()
    {
        if (string.IsNullOrEmpty(DataBaseName))
        {
            Debug.LogError("Database name is empty.");
            return;
        }

        // *** Para criar o banco em branco ou com seu sql
        //CreateDataBaseFileIfNotExists();
        
        // *** Copia um banco para a pasta do usuario
        CopyDataBaseFileIfNotExists();

        // testes;        
        try
        {
            // testa comando criar tabela
            //CreateTable();

            // testa comando inserir dados
            //InsertData("Quantos lados tem a pirâmede de Gizé?", "Quatro", "Três", "Um", "Cinco");

            // testa comando buscar dados retorna a primeira encontrada.
            //Debug.Log(GetData(2));

            // testa a deleção
            Debug.Log(DeleteData(3));
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    #region Create or Copy DataBase
    
    // Método para Copiar um banco para a pasta de execução do sistema para o usuário.
    private void CopyDataBaseFileIfNotExists()
    {
        DataBasePath = Path.Combine(Application.persistentDataPath, DataBaseName);

        if (File.Exists(DataBasePath))
            return;

        var originDataBasePath = string.Empty;
        var isAndroid = false;

#if UNITY_EDITOR || UNITY_WP8 || WIN_RP
        originDataBasePath = Path.Combine(Application.streamingAssetsPath, DataBaseName);
#elif UNITY_STANDALONE_OSX
        originDataBasePath = Path.Combine(Application.streamingAssetsPath, "/Resources/Data/StreamingAssets/", DataBaseName);
#elif UNIT_IOS
        originDataBasePath = Path.Combine(Application.streamingAssetsPath, "Raw", DataBaseName);

#elif UNITY_ANDROID
        isAndroid = true;
        originDataBasePath = "jar:file//" + Application.dataPath + "!/assets" + DataBaseName;
        StartCoroutine(GetInternalFileAndroid(originDataBasePath);
#endif
        if (!isAndroid)
            File.Copy(originDataBasePath, DataBasePath);
    }

    // Método para criar um banco zerado de acordo com o que tiver dentro do método CreateTable
    private void CreateDataBaseFileIfNotExists()
    {
        DataBasePath = Path.Combine(Application.persistentDataPath, DataBaseName);

        if (!File.Exists(DataBasePath))
        {
            SqliteConnection.CreateFile(DataBasePath);
            Debug.Log($"Database path: {DataBasePath}");

            
            // Método para criar as tabelas
            try
            {
                CreateTable();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            // Método para popular as tabelas
            try
            {
                InsertData("Quantos lados tem a pirâmede de Gizé?", "Quatro", "Três", "Um", "Cinco");
                InsertData("Qual é a cor do cavalo preto?", "preto", "branco", "amarelo", "cinza");
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    private IEnumerator GetInternalFileAndroid(string path)
    {
        var request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            Debug.LogError($"Error reading Android file: {request.error}");
        }
        else
        {
            File.WriteAllBytes(DataBasePath, request.downloadHandler.data);
            Debug.Log("File copied.");
        }
    }
    #endregion

    // Executa SQL para criação de tabelas.
    protected void CreateTable()
    {
        using (var connection = Connection)
        {
            var commandText = $"CREATE TABLE Quest"+
                $"("+
                $"      IDQuest INTEGER PRIMARY KEY, " +
                $"      Quest TEXT NOT NULL," +
                $"      questCorrect TEXT NOT NULL," +
                $"      questB TEXT NOT NULL," +
                $"      questC TEXT NOT NULL," +
                $"      questD TEXT NOT NULL," +
                $"      UNIQUE (IDQuest)" +
                $");";

            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
                Debug.Log("Command: "+ command);
            }
        }
    }

    // Executa SQL para inserção de dados nas tabelas já criadas, você pode fazer isso quando cria a tabela só
    // separei para ficar mais de boa de entender.
    protected void InsertData(string quest, string questcorrect, string questb, string questc, string questd)
    {
        var commandText = "INSERT INTO Quest(Quest, questCorrect, questB, questC, questD)" +
                          "VALUES (@quest, @questcorrect, @questb, @questc, @questd);";  
        
       // o @ é para usar o conceito de parâmetros disponível em C# nada tem haver com o SQL.

        using (var connection = Connection)
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                command.Parameters.AddWithValue("@Quest", quest);
                command.Parameters.AddWithValue("@questCorrect", questcorrect);
                command.Parameters.AddWithValue("@questB", questb);
                command.Parameters.AddWithValue("@questc", questc);
                command.Parameters.AddWithValue("@questd", questd);

                var result = command.ExecuteNonQuery();
                Debug.Log($"Insert Quest: {result.ToString()}");
            }
        }
    }

    // A consulta de dados vai depender muito de como vc quer tratar esses dados
    // o exemplo mais simples é filtrar pela id, assim você conseguirá melhorar isso ao seu rigor.
    protected string GetData(int id)
    {
        var commandText = "SELECT * FROM Quest WHERE IDQuest = @IDQuest;";
        var result = string.Empty;

        using (var connection = Connection)
        {
            connection.Open();
            using(var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddWithValue("@IDQuest", id);

                var reader = command.ExecuteReader();
                
                // cada número indica uma coluna na tabela a ultima consulta é para mostrar um jeito diferente de pegar dados
                // sem se preocupar com o tipo do dado a ser recuperado, no fim virá como string.
                if (reader.Read())
                {
                    result = $"ID: {reader.GetInt32(0).ToString()}, " +
                             $"Questão: {reader.GetString(1)} " +
                             $"Certa: {reader.GetString(2)} " +
                             $"Errada B: {reader.GetString(3)} " +
                             $"Errada C: {reader["questC"]} " +
                             $"Errada D: {reader["questD"]} ";
                }
                
                return result;
            }
        }
    }

    protected int DeleteData(int id)
    {
        var commandText = "DELETE FROM Quest WHERE IDQuest = @IDQuest;";

        using (var connection = Connection)
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddWithValue("@IDQuest", id);

                return command.ExecuteNonQuery();
            }
        }
    }
}
