//------------------------------------------------------------------------------
// <copyright file="JumpStage.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

using ACL_Gold_x64.Models;

namespace ACL_Gold_x64.Controllers
{
    public class OldJumpStage
    {/*
        public LinkedList<SkeletonPoint[]> sk_list;
        public LinkedList<Tuple<Single, Single, Single, Single>> fp_list;
        public LinkedList<DepthImagePixel[]> dp_list;
		public LinkedList<System.Windows.Media.Imaging.WriteableBitmap> im_list;
		public LinkedList<Skeleton> osk_list;
		public SkeletonPoint[] psk;
		public int jumpStatus;
        public SensorManager sensorManager;
		
        /// <summary>
        /// Constructor for the JumpStage class.
        /// </summary>
        /// <param name="sM">Sensor manager object with requisite information contained therein.</param>
        /// <param name="jumpStatus">State machine variable. Set to -1 by default</param>
        /// <param name="psk">Skeleton Point Array.</param>
		public OldJumpStage(ref SensorManager sM, int jumpStatus = -1, SkeletonPoint[] psk = null)
		{
			this.psk = psk;            
			this.jumpStatus = jumpStatus;
            sensorManager = sM;
			sk_list = new LinkedList<SkeletonPoint[]>(); //skeleton list
			im_list = new LinkedList<System.Windows.Media.Imaging.WriteableBitmap>(); //image list
			osk_list = new LinkedList<Skeleton>(); //original skeleton list
            dp_list = new LinkedList<DepthImagePixel[]>(); //depth pixel list
            fp_list = new LinkedList<Tuple<float,float,float,float>>(); //floor plane list
		}

        /// <summary>
        /// Machine state 0 = no one is in frame, or not in position to jump. default state. Only goes to state 1.
        /// Machine state 1 = some one is standing on the platform. A jump *may* take place. Can revert back to state 0.
        ///                     can only be entered from state 0.
        /// Machine state 2 = on the ground, in front of the platform. Can only enter from state 1. Must be within a certain "box". 
        ///                     2 feet wide, 2 1/2 deep.
        /// Machine state 3 = now in front of platform, feet on ground, at least 2 1/2 feet in front of platform. Jump has "ended". 
        ///  
        /// The buffer looks at the segment of data in Machine State 3. 
        /// </summary>                   
        /// <param name="skeleton">Skeleton moving about in 3D space.</param>
        /// <param name="floorClip">Floor clip tuple</param>
        /// <param name="im">WriteableBitmap</param>
        /// <param name="dp">DepthImagePixel array</param>
		public void determineJumpStage(Skeleton skeleton, Tuple<float, float, float, float> floorClip, System.Windows.Media.Imaging.WriteableBitmap im, DepthImagePixel[] dp)
		{    

			SkeletonPoint[] sk = SkeletonOperations.FloorPlaneAdjustment(skeleton, floorClip);  //Current adjusted joint data

			if (psk == null)
			{
				psk = sk;
                jumpStatus = 0;
				return;
			}

			double value = Math.Min(sk[(int)JointType.AnkleLeft].Y, sk[(int)JointType.AnkleRight].Y);
           // Console.WriteLine("HipCenter={0} Z={1}", sk[(Int32)JointType.HipCenter].Z, value);
			int goodBurger = 0;

			foreach (Joint j in skeleton.Joints)
			{
				if (j.TrackingState == JointTrackingState.Tracked)
				{
					goodBurger++;
				}
			}

			if (sk[(int)JointType.HipCenter].Z > 3.7 && goodBurger >= 17)
			{
				jumpStatus = 0;  //Reset if person is too far away
			}
			else if (jumpStatus == 0 && sk[(int)JointType.HipCenter].Z < 3.6 && sk[(int)JointType.HipCenter].Z > 2.5)
			{
				if (value > (10 * 2.54 / 100))   // Is person on platform ?
				{
					jumpStatus = 1;
					psk = sk;
				}
				else
					 jumpStatus = 0;   
			}
			else if (jumpStatus == 1)
			{                
				double centerJointXDifferential = Math.Abs(sk[(int)JointType.HipCenter].X - psk[(int)JointType.HipCenter].X) * (100 / 2.54);
				double centerJointZDifferential = (sk[(int)JointType.HipCenter].Z - psk[(int)JointType.HipCenter].Z) * (100 / 2.54);
				double footHeight = (Math.Max(sk[(int)JointType.FootRight].Y, sk[(int)JointType.FootLeft].Y)) * (100 / 2.54);

				//go to zero, check x differential
				if (centerJointXDifferential > 14 || centerJointZDifferential > 12 || centerJointZDifferential < -30)
				{
					jumpStatus = 0;
				}

				//stay at one, check x differential
				else if (footHeight > 4)
				{
					jumpStatus = 1;
				}

				//go to two
				else
				{
					jumpStatus = 2;
					psk = sk;
				}                
			}
			else if (jumpStatus == 2)
			{
				double centerJointXDifferential = Math.Abs(sk[(int)JointType.HipCenter].X - psk[(int)JointType.HipCenter].X) * (100 / 2.54);
				double centerJointZDifferential = (sk[(int)JointType.HipCenter].Z - psk[(int)JointType.HipCenter].Z) * (100 / 2.54);
				double footHeight = (Math.Max(sk[(int)JointType.FootRight].Y, sk[(int)JointType.FootLeft].Y)) * (100 / 2.54);

				//go to zero, check x differential
				if (centerJointXDifferential > 14 || centerJointZDifferential > 12 || centerJointZDifferential < -48)
				{
					jumpStatus = 0;
				}

				//go to 3 if person has moved forward at least 20 inches
				else if (centerJointZDifferential < -20)
				{
					jumpStatus = 3;
				}

				//stay in 2
				else
					jumpStatus = 2;
				 
			}
			else if (jumpStatus == 3)
			{
				jumpStatus = 0;

				Console.Write("Jump identified, extracting measures...");			
	            	

				if (sk_list.Count > 30){
					//---------------------------------------
					// Convert list to array, make combined
                    // signal...
					//---------------------------------------
					int nF = 0;
					int numJoints = Enum.GetNames(typeof(JointType)).Length;
					float[][][] skad = new float[numJoints][][];
					for (int j = 0; j < numJoints; j++){
						skad[j] = new float[3][];
						for (int d = 0; d < 3; d++){
							skad[j][d] = new float[sk_list.Count];
							nF = 0;
							foreach (SkeletonPoint[] spa in sk_list){
								if (d == 0)
									skad[j][d][nF] = spa[j].X;
								else if (d == 1)
									skad[j][d][nF] = spa[j].Y;
								else if (d == 2)
									skad[j][d][nF] = spa[j].Z;
								nF++;
							}
						}
					}

                    float[] cS = new float[sk_list.Count];
                    for (int j = 0; j < sk_list.Count; j++)
                    {
                        cS[j] = (skad[(int)JointType.AnkleLeft][1][j] + skad[(int)JointType.AnkleRight][1][j] +
                                            skad[(int)JointType.FootLeft][1][j] + skad[(int)JointType.FootRight][1][j]) / 4;
                    }

					//----------------------------------------------
					// Filter the joint locations...
                    //    CHANGE THIS TO ANSOTROPIC DIFFUSION
					//----------------------------------------------
					for (int j = 0; j < numJoints; j++){
						for (int d = 0; d < 3; d++){
							medianFilt1D(ref skad[j][d], 3);
				    		gaussFilt1D(ref skad[j][d]);   //Predefined filter...
						}
					}
                    for (int j = 0; j < 9; j++)
                    {
                        cS = JumpIdentification.anisodiff(cS, 2, 0.1, 0.225, 1);
                    }

                    //-----------------------------------------------
                    // Take derivative
                    //-----------------------------------------------
                    float[] cSd = new float[sk_list.Count];
                    cSd[0] = 0.0f;
                    int rp = 0;
                    for (int j = 1; j < sk_list.Count; j++)
                    {
                        cSd[j] = cS[j] - cS[j - 1];
                        if (cS[j] < cS[rp])
                            rp = j;  //Minimum...
                    }

					//-----------------------------------------------
					// Identify point of initial contact
					//   - Have both ankles and feet vote on
					//     point of initial contact
                    //
                    //   CHANGE THIS
					//-----------------------------------------------
                    int iC = -1;
                    for(int j = 0; j < sk_list.Count; j++){
                        if( cSd[j] > -0.019  && cS[j] < cS[rp] + 0.05 ){
                            iC = j - 1;
                            break;
                        }
                    }

					//---------------------------------------
					// Identify point of peak flexion
					//   - Have all hip joints vote...
					//---------------------------------------
					int[] tPf = { iC+1, iC+1, iC+1, iC+1 };
					int[] jtuPf = { (int)JointType.HipCenter, (int)JointType.HipLeft, (int)JointType.HipRight };
					for (int d = 0; d < 3; d++){
						float pY = 100.0f;
						float cY = skad[jtuPf[d]][1][iC];
						while (tPf[d] < nF && (cY < pY - 0.0001)){
							pY = cY;
							cY = skad[jtuPf[d]][1][tPf[d]];
							tPf[d]++;
						}
					}

					Array.Sort(tPf);
					int pF = tPf[1]-1;  // Backup 1 frame...

					Console.WriteLine("JumpSeg: " + nF + " " + iC + " " + pF);

					//------------------------------------------------
					// Do we have a plausable identification ?
					//------------------------------------------------
					if( iC > 0 && iC < nF-2 && pF < nF-2  ){

						//-------------------------------------------------
						// Measure features at points of interest...
						// Make our window with skeletons and info...
						//-------------------------------------------------
						EndOfTest eot = new EndOfTest(ref sensorManager);
						
						int f = 0;
                        double kasr_ic =0, kasr_pf=0, rV_ic=0, rV_pf=0, lV_ic=0, lV_pf=0;
						foreach (SkeletonPoint[] spa in sk_list)
						{
							LinkedList<System.Windows.Media.Imaging.WriteableBitmap>.Enumerator ime = im_list.GetEnumerator();
							LinkedList<Skeleton>.Enumerator oske = osk_list.GetEnumerator();
							for (int j = 0; j < f; j++)
							{
								ime.MoveNext();
								oske.MoveNext();
							}

							if (f == iC){
								kasr_ic = kneeAnkleRatio(spa);
								rV_ic = rightValgus(spa);
								lV_ic = leftValgus(spa);
								eot.drawSkeleton(spa, 0, ime.Current, oske.Current);
								eot.addNumbers(kasr_ic, rV_ic, lV_ic, 0);
							}
							if (f == pF){
								kasr_pf = kneeAnkleRatio(spa);
								rV_pf = rightValgus(spa);
								lV_pf = leftValgus(spa);
								eot.drawSkeleton(spa, 1, ime.Current, oske.Current);
								eot.addNumbers(kasr_pf, rV_pf, lV_pf, 1);
							}
							f++;
						}

                        int nameCounter = 0;
                        string folderName;
                        string fileName;
                        string systemTime = String.Format("{0}-{1}-{2}-{3}-{4}-{5}", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second);
                        folderName = systemTime;
                        System.IO.Directory.CreateDirectory(folderName);
                        foreach (System.Windows.Media.Imaging.WriteableBitmap bitMap in im_list)
                        {
                            if (nameCounter++ < (iC-10))
                            {
                                continue;
                            }
                            else if (nameCounter == pF)
                            {
                                fileName = System.IO.Path.Combine(folderName, nameCounter + "-" + "-peakflex" + ".bmp");
                            }
                            else if (nameCounter == iC)
                            {
                                fileName = System.IO.Path.Combine(folderName, nameCounter + "-" + "-initcontact" + ".bmp");
                            }
                            else
                            {
                                fileName = System.IO.Path.Combine(folderName, nameCounter + "-" + ".bmp");
                            }
                            using (FileStream stream5 = new FileStream(fileName, FileMode.Create))
                            {
                                PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                                encoder5.Frames.Add(BitmapFrame.Create(bitMap));
                                encoder5.Save(stream5);
                                stream5.Close();
                            }
                            
                        }

						//---------------------------
						// Finish console output
						//---------------------------
						eot.Show();
						Console.WriteLine("... done!");

                        JumpResults jump = new JumpResults();
                        foreach (SkeletonPoint[] spa in sk_list)
						{
							LinkedList<Skeleton>.Enumerator oske = osk_list.GetEnumerator();
                            LinkedList<Tuple<Single, Single, Single, Single>>.Enumerator fpe = fp_list.GetEnumerator();
                            LinkedList<DepthImagePixel[]>.Enumerator dpe = dp_list.GetEnumerator();
							for (int j = 0; j < f; j++)
							{
								oske.MoveNext();
                                fpe.MoveNext();
                                dpe.MoveNext();
							}

                            FrameData fD = new FrameData();
                            //fD.OrigSkeleton = oske.Current;
                            fD.OrigSkeletonArray = new SkeletonPoint[20];
                            //********************************************
                            //Where we put the stuff in the OrigSkeletonPoints array
                            //for each joint, put it in the equivalent index in the OrigSkeletonArray
                            
                            fD.OrigSkeletonArray[0] = oske.Current.Joints[(JointType)0].Position;   //hipCenter
                            fD.OrigSkeletonArray[1] = oske.Current.Joints[(JointType)1].Position;   //spine
                            fD.OrigSkeletonArray[2] = oske.Current.Joints[(JointType)2].Position;   //shoulderCenter
                            fD.OrigSkeletonArray[3] = oske.Current.Joints[(JointType)3].Position;   //head
                            fD.OrigSkeletonArray[4] = oske.Current.Joints[(JointType)4].Position;   //shoulderLeft
                            fD.OrigSkeletonArray[5] = oske.Current.Joints[(JointType)5].Position;   //elbowLeft
                            fD.OrigSkeletonArray[6] = oske.Current.Joints[(JointType)6].Position;   //wristLeft
                            fD.OrigSkeletonArray[7] = oske.Current.Joints[(JointType)7].Position;   //handLeft
                            fD.OrigSkeletonArray[8] = oske.Current.Joints[(JointType)8].Position;   //shoulderRight
                            fD.OrigSkeletonArray[9] = oske.Current.Joints[(JointType)9].Position;   //elbowRight
                            fD.OrigSkeletonArray[10] = oske.Current.Joints[(JointType)10].Position; //wristRight
                            fD.OrigSkeletonArray[11] = oske.Current.Joints[(JointType)11].Position; //handRight
                            fD.OrigSkeletonArray[12] = oske.Current.Joints[(JointType)12].Position; //hipLeft
                            fD.OrigSkeletonArray[13] = oske.Current.Joints[(JointType)13].Position; //kneeLeft
                            fD.OrigSkeletonArray[14] = oske.Current.Joints[(JointType)14].Position; //ankleLeft
                            fD.OrigSkeletonArray[15] = oske.Current.Joints[(JointType)15].Position; //footLeft
                            fD.OrigSkeletonArray[16] = oske.Current.Joints[(JointType)16].Position; //hipRight
                            fD.OrigSkeletonArray[17] = oske.Current.Joints[(JointType)17].Position; //kneeRight
                            fD.OrigSkeletonArray[18] = oske.Current.Joints[(JointType)18].Position; //ankleRight
                            fD.OrigSkeletonArray[19] = oske.Current.Joints[(JointType)19].Position; //footRight


                            //********************************************
                            fD.AdjustedSkeleton = spa;
                            fD.FloorPlane = fpe.Current;
                            //fD.DepthImage = dpe.Current;
                            jump.FrameDataList.Add(fD);
                        }

                        jump.InitKasr = Convert.ToSingle(kasr_ic);
                        jump.InitLeftValg = Convert.ToSingle(lV_ic);
                        jump.InitRightValg = Convert.ToSingle(rV_ic);
                        
                        jump.PeakKasr = Convert.ToSingle(kasr_pf);
                        jump.PeakLeftValg = Convert.ToSingle(lV_pf);
                        jump.PeakRightValg = Convert.ToSingle(rV_pf);

                        sensorManager.TestSubjectJumps.Jumps.Add(jump);
					}
					else{
						Console.WriteLine("... failed to correctly identify points of interest!");
					}
				}
				else{
					Console.WriteLine("... not enough data!");
				}
			}

			if (jumpStatus == 0)
			{
				sk_list.Clear();
				im_list.Clear();
				osk_list.Clear();
                fp_list.Clear();
                dp_list.Clear();
			}
			else
			{
				if (jumpStatus == 1 && sk_list.Count > 180)
				{
					sk_list.RemoveFirst();
					im_list.RemoveFirst();
					osk_list.RemoveFirst();
                    dp_list.RemoveFirst();
                    fp_list.RemoveFirst();
				}

				sk_list.AddLast(sk);
				im_list.AddLast(im.Clone());
                dp_list.AddLast(dp);
                fp_list.AddLast(floorClip);
				Skeleton tsk = new Skeleton();
				SkeletonOperations.CopySkeleton(ref tsk, skeleton);
				osk_list.AddLast(tsk);
			}

			return;
		}
        #region old code
        //----------------------------------
		//Median filter - in place
		//----------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <param name="whl"></param>
		private void medianFilt1D(ref float[] A, int whl){

			if (A.Length <= 3)
				return;

			int fS = (whl * 2) + 1;
			float[] list = new float[fS];
			float[] tA = new float[A.Length + fS - 1];
			Array.Copy(A, 0, tA, 0, whl);         //Quick and easy (bad) boundary handling..
			Array.Copy(A, 0, tA, whl, A.Length);
			Array.Copy(A, A.Length - whl, tA, A.Length + whl, whl);

			for(int j = 0; j < A.Length; j++){
				Array.Copy(tA, j, list, 0, fS);
				Array.Sort(list,0,fS);
				A[j] = list[whl];
			}
		}

		//----------------------------------
		// Fixed gaussian - in place
		//----------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
		private void gaussFilt1D(ref float[] A)
		{
			if( A.Length <= 3 )
				return;

			float[] gF = {0.017988f, 0.089096f, 0.232692f, 0.320448f, 0.232692f, 0.089096f, 0.0179880f};
			float[] tA = new float[A.Length + 6];
			Array.Copy(A, 0, tA, 0, 3);           //Quick and easy (bad) boundary handling..
			Array.Copy(A, 0, tA, 3, A.Length);
			Array.Copy(A, A.Length - 3, tA, A.Length + 3, 3);

			for (int j = 0; j < A.Length; j++){
				A[j] = 0.0f;
				for (int d = 0; d < 7; d++){
					A[j] += gF[d]*tA[j+d];
				}
			}
		}
        
#endregion

        #region revised angle algorithm

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sk"></param>
        /// <param name="dis_vect"></param>
        /// <param name="fp"></param>
        /// <returns></returns>
        private Double[] getKneeMeasures (SkeletonPoint[] sk, SkeletonPoint dis_vect, SkeletonPoint fp)
        {
            Double[] kneeMeasures = new double[7];

            //--------------------------------------------------
            //  Compute unit normal vector to frontal plane
            //      - Ignore the vertical axis (Y in this case)
            //--------------------------------------------------
            SkeletonPoint nv_fp = new SkeletonPoint();
            Double v_mag = Math.Sqrt((dis_vect.X * dis_vect.X) + (dis_vect.Z * dis_vect.Z));
            nv_fp.X = (Single)(dis_vect.X / v_mag);
            nv_fp.Y = 0;
            nv_fp.Z = (Single)(dis_vect.Z / v_mag);

            //--------------------------------------------
            //  Rotate 90 degrees to get "left" direction
            //--------------------------------------------
            SkeletonPoint nv_left = new SkeletonPoint();
            nv_left.X = (Single)(Math.Cos(Math.PI / 2.0) * nv_fp.X - Math.Sin(Math.PI / 2.0) * nv_fp.Z);
            nv_left.Y = 0;
            nv_left.Z = (Single)(Math.Sin(Math.PI / 2.0) * nv_fp.X + Math.Cos(Math.PI / 2.0) * nv_fp.Z);

            //-----------------------------------------
            // Project joints onto frontal plane
            //     p_q = q - dot(q - fp, nv_fp)*nv_fp;
            //-----------------------------------------
            SkeletonPoint[] q_sk = new SkeletonPoint[Enum.GetNames(typeof(JointType)).Length];
            for (int j = 0; j < Enum.GetNames(typeof(JointType)).Length; j++)
            {
                Double dp = dot_sk(sub_sk(sk[j], fp), nv_fp);
                q_sk[j] = sub_sk(sk[j], mul_sk(nv_fp, dp));
            }

            //------------------------------------------------
            // Left Valgus
            //------------------------------------------------
            v_mag = norm_sk(sub_sk(q_sk[(int)JointType.AnkleLeft], q_sk[(int)JointType.KneeLeft]));
            SkeletonPoint v1 = div_sk(sub_sk(q_sk[(int)JointType.AnkleLeft], q_sk[(int)JointType.KneeLeft]), v_mag);

            SkeletonPoint v2 = new SkeletonPoint();
            v2.X = 0; v2.Y = -1; v2.Z = 0;  //Unit vector straight down...

            kneeMeasures[0] = Math.Acos(dot_sk(v1, v2)) * 180.0 / Math.PI;

            // In (-) or Out (+)  ??
            Double la_nx = dot_sk(q_sk[(int)JointType.AnkleLeft], nv_left);
            Double lk_nx = dot_sk(q_sk[(int)JointType.KneeLeft], nv_left);
            if (la_nx > lk_nx)
            {
                //Knee bent in (-)
                kneeMeasures[0] = -kneeMeasures[0];
            }

            //------------------------------------------------
            // Right Valgus
            //------------------------------------------------
            v_mag = norm_sk(sub_sk(q_sk[(int)JointType.AnkleRight], q_sk[(int)JointType.KneeRight]));
            v1 = div_sk(sub_sk(q_sk[(int)JointType.AnkleRight], q_sk[(int)JointType.KneeRight]), v_mag);

            v2 = new SkeletonPoint();
            v2.X = 0; v2.Y = -1; v2.Z = 0;  //Unit vector straight down...

            kneeMeasures[1] = Math.Acos(dot_sk(v1, v2)) * 180.0 / Math.PI; //In degrees....

            // In (-) or Out (+)  ??
            Double ra_nx = dot_sk(q_sk[(int)JointType.AnkleRight], nv_left);
            Double rk_nx = dot_sk(q_sk[(int)JointType.KneeRight], nv_left);
            if (ra_nx < rk_nx)
            {
                //Knee bent in (-)
                kneeMeasures[1] = -kneeMeasures[1];
            }

            //----------------------------------------------
            // Knee / ankle seperation ratio
            //----------------------------------------------
            Double knee_sep = Math.Abs(lk_nx - rk_nx);
            Double ankle_sep = Math.Abs(la_nx - ra_nx);
            kneeMeasures[2] = knee_sep / ankle_sep;
            kneeMeasures[3] = knee_sep;
            kneeMeasures[4] = ankle_sep;

            //---------------------------------------------
            // Left/right knee X position on frontal plane
            //---------------------------------------------
            kneeMeasures[5] = lk_nx;
            kneeMeasures[6] = rk_nx;

            return kneeMeasures;
        }
        #endregion

        #region helper methods for Erik's algorithm

        /// <summary>
        /// Calculates dot products of two different skeleton point objects
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>Dot product of skeleton points p1 and p2</returns>
        Double dot_sk(SkeletonPoint p1, SkeletonPoint p2)
        {
            return (p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        SkeletonPoint sub_sk(SkeletonPoint p1, SkeletonPoint p2)
        {
            SkeletonPoint np = new SkeletonPoint();
            np.X = p1.X - p2.X;
            np.Y = p1.Y - p2.Y;
            np.Z = p1.Z - p2.Z;
            return np;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        SkeletonPoint div_sk(SkeletonPoint p1, Double val)
        {
            SkeletonPoint np = new SkeletonPoint();
            np.X = (Single)(p1.X / val);
            np.Y = (Single)(p1.Y / val);
            np.Z = (Single)(p1.Z / val);
            return np;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        SkeletonPoint mul_sk(SkeletonPoint p1, double val)
        {
            SkeletonPoint np = new SkeletonPoint();
            np.X = (Single)(p1.X * val);
            np.Y = (Single)(p1.Y * val);
            np.Z = (Single)(p1.Z * val);
            return np;
        }

        /// <summary>
        /// Normalizes skeleton point
        /// </summary>
        /// <param name="p1">Skeleton point to be normalized</param>
        /// <returns>Normalized skeleton point</returns>
        Double norm_sk(SkeletonPoint p1)
        {
            return Math.Sqrt(p1.X * p1.X + p1.Y * p1.Y + p1.Z * p1.Z);
        }

        #endregion
    }*/
    }
}
