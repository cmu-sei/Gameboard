#!/bin/bash
#
# .Synopsis Add a migration for multiple database providers
# .Notes
#     From the project root, pass in this context and migration name.
#     If you only have a single context, assign it to the parameter.
# .Example
#     ./Data/migrations-add.ps1 TestMigr ClientDb
#

if [ "$#" -ne 1 ]; then
    echo "usage: $0 migration-name"
    exit 1
fi

context=GameboardDb #${1%Context}
name=$1 #$2
declare -a providers=("SqlServer" "PostgreSQL")

for provider in "${providers[@]}"; do
    export Database__Provider=$provider
    echo $provider $name $context
    dotnet ef migrations add $name --context ${context}Context${provider} -o ./Data/Migrations/$provider/${context}
    wait
done

export Database__Provider=
