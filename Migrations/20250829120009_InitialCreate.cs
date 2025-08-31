using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplyFly.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if ContactNumber column exists before dropping it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = 'PassengerDetails' AND COLUMN_NAME = 'ContactNumber')
                BEGIN
                    ALTER TABLE PassengerDetails DROP COLUMN ContactNumber
                END
            ");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "PassengerDetails",
                newName: "Nationality");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PassengerDetails",
                newName: "PassengerId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PassengerDetails",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "PassengerDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "PassengerDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "PassengerDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatNo",
                table: "PassengerDetails",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "PassengerDetails");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "PassengerDetails");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "PassengerDetails");

            migrationBuilder.DropColumn(
                name: "SeatNo",
                table: "PassengerDetails");

            migrationBuilder.RenameColumn(
                name: "Nationality",
                table: "PassengerDetails",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "PassengerDetails",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PassengerDetails",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "PassengerDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
