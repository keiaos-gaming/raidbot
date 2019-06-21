using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord;

public class EditingFunctions
{
    public EditingFunctions()
    {
        
    }
    public bool searchFile (string name, string raid) //search file for player name, returns true if player is found
    {
        try
        {
            string line = "";
            //check if player is already signed up
            StreamReader sr = new StreamReader(raid);
            line = sr.ReadLine();
            //skip first line (summary)
            line = sr.ReadLine();
            //skip role limits
            line = sr.ReadLine();

            //loop through file
            while (line != null)
            {
                if (name == line)
                {
                    //if player is found, return true
                    sr.Close();
                    return true;
                }
                line = sr.ReadLine();
            }
            sr.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        return false;
    }

    public bool editRoles(string name, string raid, string newRoles)
    {
        string line = "";
        List<string> names = new List<string>();
        List<string> roles = new List<string>();

        try
        {
            StreamReader sr = new StreamReader(raid);
            string raidSum = sr.ReadLine();
            string roleLimits = sr.ReadLine();
            line = sr.ReadLine();

            //loop through file
            while (line != null)
            {
                if (name == line)
                {
                    names.Add(line);
                    line = sr.ReadLine();
                    roles.Add(newRoles);
                }
                else
                {
                    names.Add(line);
                    line = sr.ReadLine();
                    roles.Add(line);
                }
                line = sr.ReadLine();
            }
            sr.Close();

            StreamWriter sw = new StreamWriter(raid);
            sw.WriteLine(raidSum);
            sw.WriteLine(roleLimits);
            for (int x = 0; x < names.Count(); x++)
            {
                sw.WriteLine(names[x]);
                sw.WriteLine(roles[x]);
            }
            sw.Close();
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }

    public string getDefaults (string name)
    {
        string line = "";
        string roles = "";
        try
        {
            
            //search defaults for user
            StreamReader sr = new StreamReader("defaults.txt");
            line = sr.ReadLine();
            while (line != null)
            {
                if (name == line)
                {
                    //found user roles
                    roles = sr.ReadLine();
                }
                line = sr.ReadLine();
            }
            sr.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        if (roles == "") //default not found, return mdps as roles
            roles = "mdps ";
        return roles;
    }

    public bool addName (string raid, string name, string roles) //attempts to add player and roles to raid file, returns false if fails
    {
        try
        {
            StreamWriter sw = new StreamWriter(@raid, true);

            //Write a line of text
            sw.WriteLine(name);
            sw.WriteLine(roles);
            //close the file
            sw.Close();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
            return false;
        }
    }

    public string formatRoles (string roles)
    {
        string updatedRoles = "";
        if (roles.ToUpper().Contains("MDPS") || roles.ToUpper().Contains("MELEE")|| roles.ToUpper().Contains("MELE")|| roles.ToUpper().Contains("MELLE"))
        {
            updatedRoles += "mdps ";
        }
        if (roles.ToUpper().Contains("RDPS") || roles.ToUpper().Contains("RANGE")|| roles.ToUpper().Contains("RANGED"))
        {
            updatedRoles += "rdps ";
        }
        if (roles.ToUpper().Contains("HEALER") || roles.ToUpper().Contains("HEALS") || roles.ToUpper().Contains("HEAL"))
        {
            updatedRoles += "healer ";
        }
        if (roles.ToUpper().Contains("TANK"))
        {
            updatedRoles += "tank ";
        }
        if (updatedRoles == "")
        {
            updatedRoles = "mdps ";
        }
        return updatedRoles;
    }

    public int countSignups (string raid)
    {
        int signUps = 0;
        string line = "";
        try
        {
            //check if player is already signed up
            StreamReader sr = new StreamReader(raid);
            line = sr.ReadLine(); //first line raid infor
            line = sr.ReadLine(); //second line role limits
            
            line = sr.ReadLine(); //read first name
            //loop through file
            while (line != null)
            {
                signUps++;
                line = sr.ReadLine(); //skip roles
                line = sr.ReadLine(); //get next name
            }
            sr.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        return signUps;
    }

    public string getRoleLimits(string raid)
    {
        string line, roleLimits = "";
        try
        {
            StreamReader sr = new StreamReader(raid);
            line = sr.ReadLine(); //first line raid infor
            roleLimits = sr.ReadLine(); //second line role limits
            sr.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        return roleLimits;
    }

    public bool removePlayer (string raid, string name)
    {
        List<string> names = new List<string>();
        List<string> roles = new List<string>();
        try
        {
            StreamReader sr = new StreamReader(raid);
            string raidSum = sr.ReadLine();
            string roleLimits = sr.ReadLine();
            string line = sr.ReadLine();

            //loop through file
            while (line != null)
            {
                if (name == line)
                {
                    //if player is found, skips saving name and role for rewrite
                    line = sr.ReadLine();
                }
                else
                {
                    //if not user, adds lines to names and roles for rewrite
                    names.Add(line);
                    line = sr.ReadLine();
                    roles.Add(line);
                }
                line = sr.ReadLine();
            }
            sr.Close();

            //rewrite names and roles to file
            StreamWriter sw = new StreamWriter(raid);
            sw.WriteLine(raidSum);
            sw.WriteLine(roleLimits);
            for (int x = 0; x < names.Count(); x++)
            {
                sw.WriteLine(names[x]);
                sw.WriteLine(roles[x]);
            }
            sw.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
            return false;
        }
        return true;
    }
}