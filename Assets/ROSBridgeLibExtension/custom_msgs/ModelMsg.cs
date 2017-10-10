﻿using System.Collections;
using System.Collections.Generic;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.std_msgs;
using SimpleJSON;
using UnityEngine;


namespace ROSBridgeLib
{
    namespace custom_msgs
    {
        public class ModelMsg : ROSBridgeMsg
        {
            /// <summary>
            /// 0 stands for Removal of models
            /// 1 stands for Insertions of models
            /// </summary>
            public std_msgs.Int32Msg Operation
            {
                get
                {
                    return _Operation;
                }
            }

            /// <summary>
            /// 0 stands for World insertion/removal
            /// 1 stands for Model insertion/removal
            /// </summary>
            public std_msgs.Int32Msg Type
            {
                get
                {
                    return _Type;
                }
            }

            /// <summary>
            /// Objects which you want to remove/insert
            /// </summary>
            public custom_msgs.StringArrayMsg Objects
            {
                get
                {
                    return _Objects;
                }
            }

            /// <summary>
            /// The corresponding positions
            /// </summary>
            public custom_msgs.FloatArrayMsg Positions
            {
                get
                {
                    return _Positions;
                }
            }

            private std_msgs.Int32Msg _Operation;
            private std_msgs.Int32Msg _Type;
            private custom_msgs.StringArrayMsg _Objects;
            private custom_msgs.FloatArrayMsg _Positions;
            

            public ModelMsg(JSONNode msg)
            {
            //TODO implement in the future
            }

            public ModelMsg(int operation, int type, List<string> objects, List<Vector3> positions)
            {
                _Operation = new Int32Msg("operation", operation);
                _Type = new Int32Msg("type", type);
                _Objects = new StringArrayMsg("objects", objects);
                List<float> values = new List<float>();                
                foreach (var pos in positions)
                {
                    values.Add(pos.x);
                    values.Add(pos.y);
                    values.Add(pos.z);
                }
                _Positions = new FloatArrayMsg("positions", values);

            }

            public static string GetMessageType()
            {
                return "roboy_communication_simulation/Model";
            }

            public override string ToString()
            {
                return "roboy_communication_simulation/model [name=";
            }

            public override string ToYAMLString()
            {
                //return string.Format("{{{0}, {1}, {2}, {3}}}", _Operation.ToYAMLString(), _Type.ToYAMLString(), _Objects.ToYAMLString(), _Positions.ToYAMLString());
                return "{" + _Operation.ToYAMLString() + ", " + _Type.ToYAMLString() + ", " + _Objects.ToYAMLString() + ", " + _Positions.ToYAMLString() + "}";
            }
        }
    }
}