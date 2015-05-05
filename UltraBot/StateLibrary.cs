using System;
using System.Collections.Generic;

namespace UltraBot
{
	public class IdleState : BotAIState
	{
		public IdleState()
		{
		}

        public override void Run(Bot bot)
        {
            bot.pressButton(bot.Down());
            bot.popState();
        }
	}
    public class ThrowTechState : BotAIState
    {
        public override void Run(Bot bot)
        {
            bot.pressButton("LPLK");
            bot.popState();
        }

        public static BotAIState Trigger(Bot bot)
        {
            return bot.enemyState.AState == FighterState.AttackState.Throw ? new ThrowTechState() : null;
        }
    }
	public class SequenceState : BotAIState
	{
		[Flags]
		public enum SequenceFlags
		{
			STOP_ON_WHIFF,
			STOP_ON_BLOCK
		}
		private int index = 0;
		private List<string> Inputs = new List<string>();
		private uint timer = 0;
		public SequenceState(string sequence)
		{
			foreach(string s in sequence.Split('.'))
				Inputs.Add(s);
		}

	    public static BotAIState Trigger(Bot bot)
	    {
	        //if(bot.myState.Meter >= 250)
            //    return new SequenceState("2.3.6.6MKHK");
	        return null;
	    }

		public override void Run(Bot bot)
		{

            
			if(timer > MatchState.getInstance().FrameCounter)
			{
				return;
			}
            Console.WriteLine("{0} ->{1}", MatchState.getInstance().FrameCounter,Inputs[index]);
			if(Inputs[index][0] == 'W')
			{
				timer = UInt32.Parse(Inputs[index++].Substring(1));
                timer += MatchState.getInstance().FrameCounter;
				return;
			}

            bot.pressButton(Inputs[index++]);
            timer = MatchState.getInstance().FrameCounter+1;
            if (index > Inputs.Count - 1)
                bot.changeState(new IdleState());
		}
		
    }
    public class PokeState : BotAIState
    {
        private string _input;
        private float _distance;
        public PokeState(string input, float distance)
        {
            _input = input;
            _distance = distance;
        }

        public static BotAIState Trigger(Bot bot)
        {
            var distance = Math.Abs(bot.myState.XDistance - bot.enemyState.XDistance);
            var enemyRecoveryFrames = bot.enemyState.ScriptFrameTotal - bot.enemyState.ScriptFrameHitboxEnd;
            var enemyStartupFrames = bot.enemyState.ScriptFrameHitboxStart - 1;

            //Punish with Ultra
            if (bot.enemyState.State == FighterState.CharState.Recovery &&
                distance <= 3 &&
                bot.myState.Revenge >= 200)
            {
                return new SequenceState("6.W2.6.W15.2.6.2.6.LKMKHK"); 
            }

            //Air grab
            if (bot.enemyState.YDistance > 0)
            {
                return new SequenceState("9LPLK.9LPLK.9LPLK.9LPLK");
            }

            //Spiral Arrow Punish
            if (enemyRecoveryFrames > 19 && bot.enemyState.State == FighterState.CharState.Recovery)
            {
                return new SequenceState("2.W1.3.W1.6.LKMK");
            }

            //Punish with maximum damage no meter
            //cr.LK, cr.LP, far.LP, cr.MK xx HK Arrow
            //TODO Implement this one frame link combo
            //cr.MK xx HK Arrow, FADC, cl.HP*, cr.MK* xx HK Arrow
            if (bot.enemyState.State == FighterState.CharState.Recovery && enemyRecoveryFrames > 4 &&
                distance <= 1.2)
            {
                return new SequenceState("2LK.W8.2LP.W10.LP.W10.2MK.W8.2.W2.3.W2.6.W2.LKMK");
            }

            //Standing HK Poke 
            if (enemyRecoveryFrames > 8 &&
                bot.enemyState.State == FighterState.CharState.Recovery)
            {
                return new PokeState("HK", 2.0f);
            }

            //Standing MK Poke 
            if (enemyRecoveryFrames > 7 &&
                //bot.myState.ActiveCancelLists.Contains("GROUND") &&
                bot.enemyState.State == FighterState.CharState.Recovery ||
                     bot.enemyState.InputBuffer[0] == FighterState.Input.FORWARD)
            {
                return new PokeState("MK", 1.9f);
            }


            //Cannon Spike Punish
            if (enemyRecoveryFrames > 9 && bot.enemyState.State == FighterState.CharState.Recovery)
            {
                return new SequenceState("6.W1.2.W1.3.LKMK");
            }

            //Low MK poke 
            if (enemyRecoveryFrames > 4 && bot.enemyState.State == FighterState.CharState.Recovery)
            {
                return new PokeState("2MK", 1.7f);
            }

            //Throw Punish
            if (((enemyRecoveryFrames > 2 && bot.enemyState.State == FighterState.CharState.Recovery) ||
                (bot.enemyState.State == FighterState.CharState.Startup && enemyStartupFrames > 2) ||
                bot.enemyState.State == FighterState.CharState.Neutral ||
                bot.enemyState.AState == FighterState.AttackState.Throw) &&
                bot.myState.ActiveCancelLists.Contains("GROUND") &&
                !bot.myState.ScriptName.ToLower().Contains("upward") &&
                !bot.myState.ScriptName.ToLower().Contains("ryuken") &&
                distance < 0.8f)
            {
                return new SequenceState("4LPLK");
            }

            //Standing LP to prevent losing on chip
            if ((enemyStartupFrames < 3 && bot.enemyState.State == FighterState.CharState.Startup) ||
                (enemyRecoveryFrames > 3 && bot.enemyState.State == FighterState.CharState.Recovery) &&
                distance <= 1.5)
            {
                return new PokeState("LP", 1.5f);
            }

            //Empty jab
            //if (bot.enemyState.State == FighterState.CharState.Neutral)
            //{
            //    return new PokeState("LP", 0.9f);
            //}

            //Move forward
            if (bot.enemyState.State == FighterState.CharState.Neutral &&
                !bot.myState.ScriptName.ToLower().Contains("upward") &&
                distance > 1.7)
            {
                return new SequenceState("6");
            }

            return null;
        }

        public override void Run(Bot bot)
        {
            var spacing = Math.Abs(bot.myState.XDistance) - _distance;
            Console.WriteLine(spacing);
            if (Math.Abs(spacing) > .1)
            {
                if (spacing < 0)
                    bot.pressButton(bot.Back());
                else
                    bot.pressButton(bot.Forward());
                return;
            }
            bot.pressButton(_input);
        }
    }
    public class DefendState : BotAIState
    {
        public static BotAIState Trigger(Bot bot)
        {
            //Projectiles
            if (bot.enemyState.ScriptName.ToLower().Contains("hado") ||
                bot.enemyState.ScriptName.ToLower().Contains("jin_a") ||
                bot.enemyState.ScriptName.ToLower().Contains("jin_l") ||
                bot.enemyState.ScriptName.ToLower().Contains("sonic_"))
            {
                return new SequenceState("1.1.1.1.1.1");
            }
            //TODO Focus Attacks
            if (bot.enemyState.ScriptName.ToLower().Contains("saving_"))
            {
                return new SequenceState("4.4.4.4.4.4");
            }

            //All other moves
            if (bot.enemyState.State == FighterState.CharState.Startup || 
                bot.enemyState.State == FighterState.CharState.Active ||
                (bot.enemyState.ScriptName.ToLower().Contains("ryuken") && 
                bot.enemyState.State != FighterState.CharState.Recovery))
                return new DefendState();
            return null;
        }
        public override void Run(Bot bot)
        {
            if (bot.enemyState.State == FighterState.CharState.Startup || bot.enemyState.State == FighterState.CharState.Active)
            {
                
                if (bot.enemyState.AState != FighterState.AttackState.Throw)
                {
                    bot.pressButton(bot.Back());
                    if (bot.enemyState.AState != FighterState.AttackState.Overhead)
                        bot.pressButton(bot.Down());
                }
                else
                    bot.pressButton(bot.Up());
                Console.WriteLine("{0} {1}", bot.enemyState.ScriptName, bot.enemyState.StateTimer);
            }
            else
            {
                bot.popState();
            }

        }
    }
}
