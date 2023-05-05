using NJsonSchema;
using Gameboard.Api.GamebrainSchemaGenerator;
using NJsonSchema.Generation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Gameboard.Api.Features.GameEngine;

static string CreateSchema(Type type, JsonSchemaGeneratorSettings settings)
{
    var schema = JsonSchema.FromType(type, settings);
    return schema.ToJson();
}

static async Task<string> SaveSchema(string name, string schema, string tmpDir)
{
    var fileName = $"{name}.schema.json";
    await File.WriteAllTextAsync($"./{tmpDir}/{fileName}", schema);

    return fileName;
}

var DIR_OUT = "./schemata";
var DIR_TEMP = "./schemata-temp";
var createdSchemata = new List<string>();

if (Directory.Exists(DIR_TEMP))
    Directory.Delete(DIR_TEMP, true);
Directory.CreateDirectory(DIR_TEMP);

var genSettings = new JsonSchemaGeneratorSettings();
genSettings.GenerateAbstractSchemas = false;
genSettings.SerializerSettings = new JsonSerializerSettings()
{
    ContractResolver = new CamelCasePropertyNamesContractResolver(),
    Converters = new List<Newtonsoft.Json.JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
};

var deployContextSchemaName = "gamebrain-deploy-context";
var deployContextSchema = CreateSchema(typeof(IGamebrainDeployContext), genSettings);
var deployContextFileName = await SaveSchema(deployContextSchemaName, deployContextSchema, DIR_TEMP);
createdSchemata.Add(deployContextFileName);

var gameEngineStateSchemaName = "game-engine-state";
var gameEngineStateSchema = CreateSchema(typeof(GameEngineGameState), genSettings);
var gameEngineStateFileName = await SaveSchema(gameEngineStateSchemaName, gameEngineStateSchema, DIR_TEMP);
createdSchemata.Add(gameEngineStateFileName);

var sectionSubmissionSchemaName = "game-engine-section-submission";
var sectionSubmissionSchema = CreateSchema(typeof(GameEngineSectionSubmission), genSettings);
var sectionSubmissionFileName = await SaveSchema(sectionSubmissionSchemaName, sectionSubmissionSchema, DIR_TEMP);
createdSchemata.Add(sectionSubmissionFileName);

var challengeSchemaName = "challenge";
var challengeSchema = CreateSchema(typeof(Gameboard.Api.Data.Challenge), genSettings);
var challengeFileName = await SaveSchema(challengeSchemaName, challengeSchema, DIR_TEMP);
createdSchemata.Add(challengeFileName);

if (Directory.Exists(DIR_OUT))
    Directory.Delete(DIR_OUT, true);
Directory.CreateDirectory(DIR_OUT);

foreach (var schema in createdSchemata)
    File.Copy($"./{DIR_TEMP}/{schema}", $"./{DIR_OUT}/{schema}");

if (Directory.Exists(DIR_TEMP))
    Directory.Delete(DIR_TEMP, true);
