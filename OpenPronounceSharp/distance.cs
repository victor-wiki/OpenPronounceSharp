using Google.Protobuf.WellKnownTypes;
using NumSharp;
using NumSharp.Generic;
using NumSharp.Utilities;
using System.Security.Cryptography;
using TorchSharp;
using TransformersSharp.tokenization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OpenPronounceSharp
{
    public class distance
    {
        public static readonly bool SCIPY_ARRAY_API = Convert.ToBoolean(Environment.GetEnvironmentVariable("SCIPY_ARRAY_API")?? "False");

        /// <summary>
        /// Computes the Euclidean distance between two arrays.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        public static float euclidean(NDArray u, NDArray v, NDArray w = null)
        {
            return minkowski(u, v, p: 2, w: w);
        }

        public static float minkowski(NDArray u, NDArray v, int p = 2, NDArray w = null)
        {
            u = _asarray(u, order: 'C');
            v = _asarray(v, order: 'C');

            var u_v = u - v;

            if (w is not null)
            {
                w = _validate_weights(w);

                NDArray root_w = null;

                if (p == 1)
                {
                    root_w = w;
                }
                else if (p == 2)
                {
                    // better precision and speed
                    root_w = np.sqrt(w);
                }
                else if (p == np.inf)
                {
                    root_w = (w != 0);
                }
                else
                {
                    root_w = np.power(w, 1.0 / p);
                }

                u_v = root_w * u_v;
            }

            var dist = np.linalg.norm(u_v, ord: p, axis: -1).item<float>();

            return dist;
        }

        private static NDArray _validate_weights(NDArray w, System.Type? dtype = null)
        {
            dtype ??= np.float64;

            w = _validate_vector(w, dtype: dtype);

            if (np.any(w < 0))
            {
                throw new ValueError("Input weights should be all non-negative");
            }

            return w;
        }

        private static NDArray _validate_vector(NDArray u, System.Type? dtype = null)
        {
            // XXX Is order='c' really necessary?
            u = np.asarray(u, dtype: dtype, order: 'c');

            if (u.ndim == 1)
            {
                return u;
            }

            throw new ValueError("Input vector should be 1-D.");
        }

        /// <summary>
        /// SciPy-specific replacement for `np.asarray` with `order`, `check_finite`, and `subok`.
        /// 
        /// Memory layout parameter `order` is not exposed in the Array API standard.
        /// `order` is only enforced if the input array implementation
        /// is NumPy based, otherwise `order` is just silently ignored.
        ///
        /// `check_finite` is also not a keyword in the array API standard; included
        /// here for convenience rather than that having to be a separate function
        /// call inside SciPy functions.
        ///
        /// `subok` is included to allow this function to preserve the behaviour of
        /// `np.asanyarray` for NumPy based inputs.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="dtype"></param>
        /// <param name="order"></param>
        /// <param name="copy"></param>
        /// <param name="xp"></param>
        /// <param name="check_finite"></param>
        /// <param name="subok"></param>
        /// <returns></returns>
        private static NDArray _asarray(NDArray array, System.Type? dtype = null, char order = 'C',
                                        bool? copy = null, dynamic xp = null, bool check_finite = false, bool subok = false)
        {
            if(copy == true)
            {
                //array = np.array(array, order: order, dtype: dtype, subok: subok*); ////to do
            }
            else if(subok == true)
            {
                array = np.asanyarray(array, order: order, dtype: dtype);
            }
            else
            {
                array = np.asarray(array, order: order, dtype: dtype);
            }

            if(check_finite)
            {
                ////to do
            }

            return array;           
        }      
    }
}
