#include common_scripts\utility;
#include maps\_utility;

init()
{
	level thread onPlayerConnect();
}

onPlayerConnect()
{
	for(;;)
	{
		level waittill( "connected", player );
		
		player thread onPlayerSpawned();
	}
}

onPlayerSpawned()
{
	for(;;)
	{
		self waittill("spawned_player");
		
		self thread duke_monitorButtons();
	}
}

duke_monitorButtons()
{
	self endon("death");
	
	for(;;)
	{
		if(self useButtonPressed() && self getStance() == "prone")
		{
			self iprintln("^5Square button pressed");
			
			//Put your custom GSC code in this file :)
			
			wait .5;
		}
		
		wait .01;
	}
}